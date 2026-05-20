using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace ProTranslate.Avalonia;

public sealed class Translation
{
    public static readonly AttachedProperty<CultureInfo?> CultureProperty =
        AvaloniaProperty.RegisterAttached<Translation, Visual, CultureInfo?>(
            "Culture",
            null,
            inherits: true);

    public static readonly AttachedProperty<bool> AutoFlowDirectionProperty =
        AvaloniaProperty.RegisterAttached<Translation, Visual, bool>(
            "AutoFlowDirection",
            false,
            inherits: true);

    public static readonly AttachedProperty<string?> KeyProperty =
        AvaloniaProperty.RegisterAttached<Translation, AvaloniaObject, string?>(
            "Key",
            null);

    public static readonly AttachedProperty<object?> FallbackValueProperty =
        AvaloniaProperty.RegisterAttached<Translation, AvaloniaObject, object?>(
            "FallbackValue",
            null);

    public static readonly AttachedProperty<string?> StringFormatProperty =
        AvaloniaProperty.RegisterAttached<Translation, AvaloniaObject, string?>(
            "StringFormat",
            null);

    private static readonly ConditionalWeakTable<AvaloniaObject, AttachedTranslationSubscription> AttachedTargets = new();

    private Translation()
    {
    }

    static Translation()
    {
        CultureProperty.Changed.AddClassHandler<Visual>((visual, args) =>
        {
            if (args.NewValue is CultureInfo culture)
            {
                TranslationService.Culture = culture;
                ApplyAutoFlowDirection(visual, culture);
            }
        });
        AutoFlowDirectionProperty.Changed.AddClassHandler<Visual>((visual, _) =>
        {
            ApplyAutoFlowDirection(visual, GetCulture(visual) ?? TranslationService.Culture);
            UpdateAttachedSubscription(visual);
        });
        KeyProperty.Changed.AddClassHandler<AvaloniaObject>((element, _) => UpdateAttachedSubscription(element));
        FallbackValueProperty.Changed.AddClassHandler<AvaloniaObject>((element, _) => RefreshAttachedTarget(element));
        StringFormatProperty.Changed.AddClassHandler<AvaloniaObject>((element, _) => RefreshAttachedTarget(element));
    }

    public static CultureInfo? GetCulture(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        return visual.GetValue(CultureProperty);
    }

    public static void SetCulture(Visual visual, CultureInfo? value)
    {
        ArgumentNullException.ThrowIfNull(visual);
        visual.SetValue(CultureProperty, value);
    }

    public static bool GetAutoFlowDirection(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        return visual.GetValue(AutoFlowDirectionProperty);
    }

    public static void SetAutoFlowDirection(Visual visual, bool value)
    {
        ArgumentNullException.ThrowIfNull(visual);
        visual.SetValue(AutoFlowDirectionProperty, value);

        var culture = GetCulture(visual) ?? TranslationService.Culture;
        Visual.SetFlowDirection(visual, ToFlowDirection(culture));
    }

    public static string? GetKey(AvaloniaObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(KeyProperty);
    }

    public static void SetKey(AvaloniaObject element, string? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(KeyProperty, value);
    }

    public static object? GetFallbackValue(AvaloniaObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(FallbackValueProperty);
    }

    public static void SetFallbackValue(AvaloniaObject element, object? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(FallbackValueProperty, value);
    }

    public static string? GetStringFormat(AvaloniaObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(StringFormatProperty);
    }

    public static void SetStringFormat(AvaloniaObject element, string? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(StringFormatProperty, value);
    }

    public static FlowDirection ToFlowDirection(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    private static void UpdateAttachedSubscription(AvaloniaObject element)
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

    private static bool RequiresAttachedSubscription(AvaloniaObject element) =>
        !string.IsNullOrWhiteSpace(GetKey(element))
        || element is Visual visual && GetAutoFlowDirection(visual);

    private static void RefreshAttachedTarget(AvaloniaObject element)
    {
        UpdateAttachedText(element);
        ApplyAutoFlowDirection(element);
    }

    private static void ApplyAutoFlowDirection(AvaloniaObject element)
    {
        if (element is Visual visual)
        {
            ApplyAutoFlowDirection(visual, GetCulture(visual) ?? TranslationService.Culture);
        }
    }

    private static void ApplyAutoFlowDirection(Visual visual, CultureInfo culture)
    {
        if (GetAutoFlowDirection(visual))
        {
            Visual.SetFlowDirection(visual, ToFlowDirection(culture));
        }
    }

    private static bool TryDispatchRefresh(AvaloniaObject element)
    {
        try
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                RefreshAttachedTarget(element);
                return true;
            }

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    RefreshAttachedTarget(element);
                }
                catch (InvalidOperationException)
                {
                }
            });
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static void UpdateAttachedText(AvaloniaObject element)
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
            case TextBox textBox:
                textBox.Text = value?.ToString();
                break;
            case ContentControl contentControl:
                contentControl.Content = value;
                break;
        }
    }

    private sealed class AttachedTranslationSubscription : IDisposable
    {
        private readonly WeakReference<AvaloniaObject> _target;
        private TranslationBindingSource _source;
        private bool _disposed;

        public AttachedTranslationSubscription(AvaloniaObject target)
        {
            _target = new WeakReference<AvaloniaObject>(target);
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

            if (_target.TryGetTarget(out AvaloniaObject? target))
            {
                if (!TryDispatchRefresh(target))
                {
                    Dispose();
                }
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

            if (_target.TryGetTarget(out AvaloniaObject? target))
            {
                if (!TryDispatchRefresh(target))
                {
                    Dispose();
                }
            }
            else
            {
                Dispose();
            }
        }
    }
}
