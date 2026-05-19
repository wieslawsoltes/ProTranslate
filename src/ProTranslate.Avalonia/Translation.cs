using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

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

                if (GetAutoFlowDirection(visual))
                {
                    Visual.SetFlowDirection(visual, ToFlowDirection(culture));
                }
            }
        });
        KeyProperty.Changed.AddClassHandler<AvaloniaObject>((element, _) => UpdateAttachedSubscription(element));
        FallbackValueProperty.Changed.AddClassHandler<AvaloniaObject>((element, _) => UpdateAttachedText(element));
        StringFormatProperty.Changed.AddClassHandler<AvaloniaObject>((element, _) => UpdateAttachedText(element));
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
        if (string.IsNullOrWhiteSpace(GetKey(element)))
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
        private bool _disposed;

        public AttachedTranslationSubscription(AvaloniaObject target)
        {
            _target = new WeakReference<AvaloniaObject>(target);
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

            if (_target.TryGetTarget(out AvaloniaObject? target))
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
