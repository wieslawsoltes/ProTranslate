using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ProTranslate.Wpf;

[MarkupExtensionReturnType(typeof(string))]
public sealed class TranslateExtension : MarkupExtension
{
    public TranslateExtension()
    {
    }

    public TranslateExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string? Key { get; set; }

    public string? Path
    {
        get => Key;
        set => Key = value;
    }

    public object? FallbackValue { get; set; }

    public string? StringFormat { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var key = Key ?? string.Empty;
        var binding = new Binding($"[{key}]")
        {
            Source = TranslationService.Source,
            Mode = BindingMode.OneWay,
            FallbackValue = FallbackValue,
            StringFormat = StringFormat
        };

        return binding.ProvideValue(serviceProvider);
    }
}

[MarkupExtensionReturnType(typeof(string))]
public sealed class TExtension : MarkupExtension
{
    private readonly TranslateExtension _inner = new();

    public TExtension()
    {
    }

    public TExtension(string key)
    {
        _inner.Key = key;
    }

    [ConstructorArgument("key")]
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

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return _inner.ProvideValue(serviceProvider);
    }
}

[MarkupExtensionReturnType(typeof(string))]
public sealed class FormatExtension : MarkupExtension
{
    public FormatExtension()
    {
    }

    public FormatExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string? Key { get; set; }

    public string? Path
    {
        get => Key;
        set => Key = value;
    }

    public object? Value { get; set; }

    public object? FallbackValue { get; set; }

    public string? StringFormat { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var key = Key ?? string.Empty;

        if (Value is null)
        {
            var textBinding = new Binding($"[{key}]")
            {
                Source = TranslationService.Source,
                Mode = BindingMode.OneWay,
                FallbackValue = FallbackValue,
                StringFormat = StringFormat
            };

            return textBinding.ProvideValue(serviceProvider);
        }

        var binding = new MultiBinding
        {
            Converter = FormatValueConverter.Instance,
            ConverterParameter = new FormatValueConverterParameter(key, StringFormat),
            FallbackValue = FallbackValue
        };
        binding.Bindings.Add(new Binding($"[{key}]")
        {
            Source = TranslationService.Source,
            Mode = BindingMode.OneWay
        });
        binding.Bindings.Add(Value is BindingBase valueBinding ? valueBinding : new Binding(nameof(ConstantValueSource.Value))
        {
            Source = new ConstantValueSource(Value),
            Mode = BindingMode.OneWay
        });

        return binding.ProvideValue(serviceProvider);
    }

    private sealed class ConstantValueSource(object? value)
    {
        public object? Value { get; } = value;
    }

    private sealed record FormatValueConverterParameter(string Key, string? StringFormat);

    private sealed class FormatValueConverter : IMultiValueConverter
    {
        public static readonly FormatValueConverter Instance = new();

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not FormatValueConverterParameter formatParameter)
            {
                return Binding.DoNothing;
            }

            if (values.Length > 1 && ReferenceEquals(values[1], DependencyProperty.UnsetValue))
            {
                return Binding.DoNothing;
            }

            object? result = values.Length <= 1
                ? TranslationService.Source.Translate(formatParameter.Key)
                : TranslationService.Source.Translate(formatParameter.Key, values.Skip(1).ToArray());

            return formatParameter.StringFormat is null
                ? result
                : string.Format(culture, formatParameter.StringFormat, result);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return targetTypes.Select(_ => Binding.DoNothing).ToArray();
        }
    }
}

[MarkupExtensionReturnType(typeof(string))]
public sealed class FExtension : MarkupExtension
{
    private readonly FormatExtension _inner = new();

    public FExtension()
    {
    }

    public FExtension(string key)
    {
        _inner.Key = key;
    }

    [ConstructorArgument("key")]
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

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return _inner.ProvideValue(serviceProvider);
    }
}
