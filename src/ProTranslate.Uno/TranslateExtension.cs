using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;

namespace ProTranslate.Uno;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public class TranslateExtension : MarkupExtension
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

    protected override object ProvideValue(IXamlServiceProvider serviceProvider)
    {
        var key = Key ?? string.Empty;

        return new Binding
        {
            Source = TranslationService.Source,
            Path = new PropertyPath($"[{key}]"),
            Mode = BindingMode.OneWay,
            FallbackValue = FallbackValue,
            Converter = StringFormat is null ? null : StringFormatConverter.Instance,
            ConverterParameter = StringFormat
        };
    }
}

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed class TExtension : TranslateExtension
{
    public TExtension()
    {
    }

    public TExtension(string key)
        : base(key)
    {
    }
}

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public class FormatExtension : MarkupExtension
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

    protected override object ProvideValue(IXamlServiceProvider serviceProvider)
    {
        var key = Key ?? string.Empty;

        if (Value is Binding valueBinding)
        {
            IValueConverter? valueConverter = valueBinding.Converter;
            object? valueConverterParameter = valueBinding.ConverterParameter;
            valueBinding.Converter = FormatValueConverter.Instance;
            valueBinding.ConverterParameter = new FormatValueConverterParameter(
                key,
                null,
                StringFormat,
                UseBoundValue: true,
                valueConverter,
                valueConverterParameter);
            valueBinding.FallbackValue = FallbackValue;
            return valueBinding;
        }

        object? value = Value;

        return new Binding
        {
            Source = TranslationService.Source,
            Path = new PropertyPath($"[{key}]"),
            Mode = BindingMode.OneWay,
            FallbackValue = FallbackValue,
            Converter = value is null && StringFormat is null ? null : FormatValueConverter.Instance,
            ConverterParameter = new FormatValueConverterParameter(key, value, StringFormat, UseBoundValue: false)
        };
    }
}

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed class FExtension : FormatExtension
{
    public FExtension()
    {
    }

    public FExtension(string key)
        : base(key)
    {
    }
}

internal sealed class StringFormatConverter : IValueConverter
{
    public static readonly StringFormatConverter Instance = new();

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return parameter is string stringFormat
            ? string.Format(TranslationService.Source.TranslationService.CurrentCulture, stringFormat, value)
            : value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}

internal sealed record FormatValueConverterParameter(
    string Key,
    object? Value,
    string? StringFormat,
    bool UseBoundValue,
    IValueConverter? BoundValueConverter = null,
    object? BoundValueConverterParameter = null);

internal sealed class FormatValueConverter : IValueConverter
{
    public static readonly FormatValueConverter Instance = new();

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is not FormatValueConverterParameter formatParameter)
        {
            return value;
        }

        if (IsUnset(value))
        {
            return DependencyProperty.UnsetValue;
        }

        object? result;
        if (formatParameter.UseBoundValue)
        {
            object? boundValue = formatParameter.BoundValueConverter is null
                ? value
                : formatParameter.BoundValueConverter.Convert(
                    value,
                    typeof(object),
                    formatParameter.BoundValueConverterParameter,
                    language);

            if (IsUnset(boundValue))
            {
                return DependencyProperty.UnsetValue;
            }

            result = TranslationService.Source.Translate(formatParameter.Key, boundValue);
        }
        else
        {
            result = formatParameter.Value is null
                ? TranslationService.Source.Translate(formatParameter.Key)
                : TranslationService.Source.Translate(formatParameter.Key, formatParameter.Value);
        }

        return formatParameter.StringFormat is null
            ? result
            : string.Format(TranslationService.Source.TranslationService.CurrentCulture, formatParameter.StringFormat, result);
    }

    private static bool IsUnset(object? value)
    {
        return ReferenceEquals(value, DependencyProperty.UnsetValue);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
