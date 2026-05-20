using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace ProTranslate.WinUI;

public static class Translation
{
    public static readonly DependencyProperty CultureProperty =
        DependencyProperty.RegisterAttached(
            "Culture",
            typeof(CultureInfo),
            typeof(Translation),
            new PropertyMetadata(null, OnCultureChanged));

    public static readonly DependencyProperty AutoFlowDirectionProperty =
        DependencyProperty.RegisterAttached(
            "AutoFlowDirection",
            typeof(bool),
            typeof(Translation),
            new PropertyMetadata(false, OnAutoFlowDirectionChanged));

    public static readonly DependencyProperty KeyProperty =
        DependencyProperty.RegisterAttached(
            "Key",
            typeof(string),
            typeof(Translation),
            new PropertyMetadata(null, OnKeyChanged));

    public static readonly DependencyProperty FallbackValueProperty =
        DependencyProperty.RegisterAttached(
            "FallbackValue",
            typeof(object),
            typeof(Translation),
            new PropertyMetadata(null, OnAttachedTextOptionChanged));

    public static readonly DependencyProperty StringFormatProperty =
        DependencyProperty.RegisterAttached(
            "StringFormat",
            typeof(string),
            typeof(Translation),
            new PropertyMetadata(null, OnAttachedTextOptionChanged));

    private static readonly ConditionalWeakTable<DependencyObject, AttachedTranslationSubscription> AttachedTargets = new();

    public static CultureInfo? GetCulture(DependencyObject element)
    {
        return (CultureInfo?)element.GetValue(CultureProperty);
    }

    public static void SetCulture(DependencyObject element, CultureInfo? value)
    {
        element.SetValue(CultureProperty, value);
    }

    public static bool GetAutoFlowDirection(DependencyObject element)
    {
        return (bool)element.GetValue(AutoFlowDirectionProperty);
    }

    public static void SetAutoFlowDirection(DependencyObject element, bool value)
    {
        element.SetValue(AutoFlowDirectionProperty, value);
    }

    public static string? GetKey(DependencyObject element)
    {
        return (string?)element.GetValue(KeyProperty);
    }

    public static void SetKey(DependencyObject element, string? value)
    {
        element.SetValue(KeyProperty, value);
    }

    public static object? GetFallbackValue(DependencyObject element)
    {
        return element.GetValue(FallbackValueProperty);
    }

    public static void SetFallbackValue(DependencyObject element, object? value)
    {
        element.SetValue(FallbackValueProperty, value);
    }

    public static string? GetStringFormat(DependencyObject element)
    {
        return (string?)element.GetValue(StringFormatProperty);
    }

    public static void SetStringFormat(DependencyObject element, string? value)
    {
        element.SetValue(StringFormatProperty, value);
    }

    public static FlowDirection ToFlowDirection(CultureInfo culture)
    {
        return culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    private static void OnCultureChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not CultureInfo culture)
        {
            return;
        }

        TranslationService.Culture = culture;

        if (element is FrameworkElement frameworkElement)
        {
            frameworkElement.Language = culture.IetfLanguageTag;
        }

        if (GetAutoFlowDirection(element) && element is FrameworkElement flowElement)
        {
            ApplyAutoFlowDirection(flowElement, culture);
        }
    }

    private static void OnAutoFlowDirectionChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && element is FrameworkElement frameworkElement)
        {
            frameworkElement.FlowDirection = ToFlowDirection(GetCulture(element) ?? TranslationService.Culture);
        }

        UpdateAttachedSubscription(element);
    }

    private static void OnKeyChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        UpdateAttachedSubscription(element);
    }

    private static void UpdateAttachedSubscription(DependencyObject element)
    {
        if (!RequiresAttachedSubscription(element))
        {
            if (AttachedTargets.TryGetValue(element, out AttachedTranslationSubscription? subscription))
            {
                subscription.Dispose();
                AttachedTargets.Remove(element);
            }

            RefreshAttachedTarget(element);
            return;
        }

        AttachedTargets.GetValue(element, static target => new AttachedTranslationSubscription(target));
        RefreshAttachedTarget(element);
    }

    private static bool RequiresAttachedSubscription(DependencyObject element) =>
        !string.IsNullOrWhiteSpace(GetKey(element)) || GetAutoFlowDirection(element);

    private static void OnAttachedTextOptionChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        RefreshAttachedTarget(element);
    }

    private static void RefreshAttachedTarget(DependencyObject element)
    {
        UpdateAttachedText(element);
        ApplyAutoFlowDirection(element);
    }

    private static void ApplyAutoFlowDirection(DependencyObject element)
    {
        if (element is FrameworkElement frameworkElement)
        {
            ApplyAutoFlowDirection(frameworkElement, GetCulture(element) ?? TranslationService.Culture);
        }
    }

    private static void ApplyAutoFlowDirection(FrameworkElement element, CultureInfo culture)
    {
        if (GetAutoFlowDirection(element))
        {
            element.FlowDirection = ToFlowDirection(culture);
        }
    }

    private static void DispatchRefresh(DependencyObject element)
    {
        var dispatcherQueue = element.DispatcherQueue;
        if (dispatcherQueue is null || dispatcherQueue.HasThreadAccess)
        {
            RefreshAttachedTarget(element);
            return;
        }

        dispatcherQueue.TryEnqueue(() => RefreshAttachedTarget(element));
    }

    private static void UpdateAttachedText(DependencyObject element)
    {
        string? key = GetKey(element);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var localized = TranslationService.Source.TranslationService.GetString(key);
        object? value = localized.ResourceNotFound && GetFallbackValue(element) is { } fallback
            ? fallback
            : localized.Value;

        if (GetStringFormat(element) is { } stringFormat)
        {
            value = string.Format(TranslationService.Source.TranslationService.CurrentCulture, stringFormat, value);
        }

        switch (element)
        {
            case TextBlock textBlock:
                textBlock.Text = value?.ToString();
                break;
            case ContentControl contentControl:
                contentControl.Content = value;
                break;
        }
    }

    private sealed class AttachedTranslationSubscription : IDisposable
    {
        private readonly WeakReference<DependencyObject> _target;
        private TranslationBindingSource _source;
        private bool _disposed;

        public AttachedTranslationSubscription(DependencyObject target)
        {
            _target = new WeakReference<DependencyObject>(target);
            _source = TranslationService.Source;
            _source.PropertyChanged += OnSourcePropertyChanged;
            TranslationService.SourceChanged += OnSourceChanged;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _source.PropertyChanged -= OnSourcePropertyChanged;
            TranslationService.SourceChanged -= OnSourceChanged;
            _disposed = true;
        }

        private void OnSourceChanged(object? sender, EventArgs e)
        {
            _source.PropertyChanged -= OnSourcePropertyChanged;
            _source = TranslationService.Source;
            _source.PropertyChanged += OnSourcePropertyChanged;

            if (_target.TryGetTarget(out DependencyObject? target))
            {
                DispatchRefresh(target);
            }
            else
            {
                Dispose();
            }
        }

        private void OnSourcePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not "Item[]" and not nameof(TranslationBindingSource.Culture))
            {
                return;
            }

            if (_target.TryGetTarget(out DependencyObject? target))
            {
                DispatchRefresh(target);
            }
            else
            {
                Dispose();
            }
        }
    }
}
