using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace ProTranslate.Maui;

[AcceptEmptyServiceProvider]
[ContentProperty(nameof(Key))]
public sealed class TranslateExtension : IMarkupExtension<BindingBase>
{
    public TranslateExtension()
    {
    }

    public TranslateExtension(string key)
    {
        Key = key;
    }

    public string? Key { get; set; }

    public string? Path
    {
        get => Key;
        set => Key = value;
    }

    public object? FallbackValue { get; set; }

    public string? StringFormat { get; set; }

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        var key = Key ?? string.Empty;

        return new Binding($"[{key}]", BindingMode.OneWay, source: TranslationService.Source)
        {
            FallbackValue = FallbackValue,
            StringFormat = StringFormat
        };
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}

[AcceptEmptyServiceProvider]
[ContentProperty(nameof(Key))]
public sealed class FormatExtension : IMarkupExtension<BindingBase>
{
    public FormatExtension()
    {
    }

    public FormatExtension(string key)
    {
        Key = key;
    }

    public string? Key { get; set; }

    public string? Path
    {
        get => Key;
        set => Key = value;
    }

    public object? Value { get; set; }

    public object? FallbackValue { get; set; }

    public string? StringFormat { get; set; }

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        var key = Key ?? string.Empty;

        if (Value is null)
        {
            return new Binding($"[{key}]", BindingMode.OneWay, source: TranslationService.Source)
            {
                FallbackValue = FallbackValue,
                StringFormat = StringFormat
            };
        }

        var binding = new MultiBinding
        {
            Converter = FormatValueConverter.Instance,
            ConverterParameter = new FormatValueConverterParameter(key, StringFormat),
            FallbackValue = FallbackValue
        };
        binding.Bindings.Add(new Binding($"[{key}]", BindingMode.OneWay, source: TranslationService.Source));
        binding.Bindings.Add(Value is BindingBase valueBinding ? valueBinding : new Binding(nameof(ConstantValueSource.Value), BindingMode.OneWay, source: new ConstantValueSource(Value)));

        return binding;
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }

    private sealed class ConstantValueSource(object? value)
    {
        public object? Value { get; } = value;
    }

    private sealed record FormatValueConverterParameter(string Key, string? StringFormat);

    private sealed class FormatValueConverter : IMultiValueConverter
    {
        public static readonly FormatValueConverter Instance = new();

        public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is not FormatValueConverterParameter formatParameter)
            {
                return null;
            }

            if (values.Any(IsUnset))
            {
                return BindableProperty.UnsetValue;
            }

            object? result = values.Length <= 1
                ? TranslationService.Source.Translate(formatParameter.Key)
                : TranslationService.Source.Translate(formatParameter.Key, values.Skip(1).ToArray());

            return formatParameter.StringFormat is null
                ? result
                : string.Format(culture, formatParameter.StringFormat, result);
        }

        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            return targetTypes.Select(_ => Binding.DoNothing).ToArray();
        }

        private static bool IsUnset(object? value)
        {
            return ReferenceEquals(value, BindableProperty.UnsetValue) ||
                   ReferenceEquals(value, Binding.DoNothing);
        }
    }
}

[AcceptEmptyServiceProvider]
[ContentProperty(nameof(Key))]
public sealed class FExtension : IMarkupExtension<BindingBase>
{
    private readonly FormatExtension _inner = new();

    public FExtension()
    {
    }

    public FExtension(string key)
    {
        _inner.Key = key;
    }

    public string? Key
    {
        get => _inner.Key;
        set => _inner.Key = value;
    }

    public string? Path
    {
        get => _inner.Path;
        set => _inner.Path = value;
    }

    public object? Value
    {
        get => _inner.Value;
        set => _inner.Value = value;
    }

    public object? FallbackValue
    {
        get => _inner.FallbackValue;
        set => _inner.FallbackValue = value;
    }

    public string? StringFormat
    {
        get => _inner.StringFormat;
        set => _inner.StringFormat = value;
    }

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        return _inner.ProvideValue(serviceProvider);
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}

[AcceptEmptyServiceProvider]
[ContentProperty(nameof(Key))]
public sealed class TExtension : IMarkupExtension<BindingBase>
{
    private readonly TranslateExtension _inner = new();

    public TExtension()
    {
    }

    public TExtension(string key)
    {
        _inner.Key = key;
    }

    public string? Key
    {
        get => _inner.Key;
        set => _inner.Key = value;
    }

    public string? Path
    {
        get => _inner.Path;
        set => _inner.Path = value;
    }

    public object? FallbackValue
    {
        get => _inner.FallbackValue;
        set => _inner.FallbackValue = value;
    }

    public string? StringFormat
    {
        get => _inner.StringFormat;
        set => _inner.StringFormat = value;
    }

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        return _inner.ProvideValue(serviceProvider);
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}
