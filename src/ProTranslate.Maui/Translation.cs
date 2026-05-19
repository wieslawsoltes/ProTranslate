using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace ProTranslate.Maui;

public static class Translation
{
    public static readonly BindableProperty CultureProperty =
        BindableProperty.CreateAttached(
            "Culture",
            typeof(CultureInfo),
            typeof(Translation),
            null,
            propertyChanged: OnCultureChanged);

    public static readonly BindableProperty AutoFlowDirectionProperty =
        BindableProperty.CreateAttached(
            "AutoFlowDirection",
            typeof(bool),
            typeof(Translation),
            false,
            propertyChanged: OnAutoFlowDirectionChanged);

    public static readonly BindableProperty KeyProperty =
        BindableProperty.CreateAttached(
            "Key",
            typeof(string),
            typeof(Translation),
            null,
            propertyChanged: OnKeyChanged);

    public static readonly BindableProperty FallbackValueProperty =
        BindableProperty.CreateAttached(
            "FallbackValue",
            typeof(object),
            typeof(Translation),
            null,
            propertyChanged: OnAttachedTextOptionChanged);

    public static readonly BindableProperty StringFormatProperty =
        BindableProperty.CreateAttached(
            "StringFormat",
            typeof(string),
            typeof(Translation),
            null,
            propertyChanged: OnAttachedTextOptionChanged);

    private static readonly ConditionalWeakTable<BindableObject, AttachedTranslationSubscription> AttachedTargets = new();

    public static CultureInfo? GetCulture(BindableObject element)
    {
        return (CultureInfo?)element.GetValue(CultureProperty);
    }

    public static void SetCulture(BindableObject element, CultureInfo? value)
    {
        element.SetValue(CultureProperty, value);
    }

    public static bool GetAutoFlowDirection(BindableObject element)
    {
        return (bool)element.GetValue(AutoFlowDirectionProperty);
    }

    public static void SetAutoFlowDirection(BindableObject element, bool value)
    {
        element.SetValue(AutoFlowDirectionProperty, value);
    }

    public static string? GetKey(BindableObject element)
    {
        return (string?)element.GetValue(KeyProperty);
    }

    public static void SetKey(BindableObject element, string? value)
    {
        element.SetValue(KeyProperty, value);
    }

    public static object? GetFallbackValue(BindableObject element)
    {
        return element.GetValue(FallbackValueProperty);
    }

    public static void SetFallbackValue(BindableObject element, object? value)
    {
        element.SetValue(FallbackValueProperty, value);
    }

    public static string? GetStringFormat(BindableObject element)
    {
        return (string?)element.GetValue(StringFormatProperty);
    }

    public static void SetStringFormat(BindableObject element, string? value)
    {
        element.SetValue(StringFormatProperty, value);
    }

    public static FlowDirection ToFlowDirection(CultureInfo culture)
    {
        return culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    private static void OnCultureChanged(BindableObject element, object oldValue, object newValue)
    {
        if (newValue is not CultureInfo culture)
        {
            return;
        }

        TranslationService.Culture = culture;

        if (GetAutoFlowDirection(element) && element is VisualElement visualElement)
        {
            visualElement.FlowDirection = ToFlowDirection(culture);
        }
    }

    private static void OnAutoFlowDirectionChanged(BindableObject element, object oldValue, object newValue)
    {
        if (newValue is true && element is VisualElement visualElement)
        {
            visualElement.FlowDirection = ToFlowDirection(GetCulture(element) ?? TranslationService.Culture);
        }
    }

    private static void OnKeyChanged(BindableObject element, object oldValue, object newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue as string))
        {
            if (AttachedTargets.TryGetValue(element, out AttachedTranslationSubscription? subscription))
            {
                subscription.Dispose();
                AttachedTargets.Remove(element);
            }

            UpdateAttachedText(element);
            return;
        }

        AttachedTargets.GetValue(element, static target => new AttachedTranslationSubscription(target));
        UpdateAttachedText(element);
    }

    private static void OnAttachedTextOptionChanged(BindableObject element, object oldValue, object newValue)
    {
        UpdateAttachedText(element);
    }

    private static void UpdateAttachedText(BindableObject element)
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
            case Label label:
                label.Text = value?.ToString();
                break;
            case Button button:
                button.Text = value?.ToString();
                break;
            case Entry entry:
                entry.Text = value?.ToString();
                break;
            case Editor editor:
                editor.Text = value?.ToString();
                break;
            case SearchBar searchBar:
                searchBar.Text = value?.ToString();
                break;
        }
    }

    private sealed class AttachedTranslationSubscription : IDisposable
    {
        private readonly WeakReference<BindableObject> _target;
        private bool _disposed;

        public AttachedTranslationSubscription(BindableObject target)
        {
            _target = new WeakReference<BindableObject>(target);
            TranslationService.Source.PropertyChanged += OnSourcePropertyChanged;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            TranslationService.Source.PropertyChanged -= OnSourcePropertyChanged;
            _disposed = true;
        }

        private void OnSourcePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not "Item[]" and not nameof(TranslationBindingSource.Culture))
            {
                return;
            }

            if (_target.TryGetTarget(out BindableObject? target))
            {
                UpdateAttachedText(target);
            }
            else
            {
                Dispose();
            }
        }
    }
}
