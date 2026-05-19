using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;

namespace ProTranslate.WinUI;

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
        object? value = Value is Binding ? null : Value;

        return new Binding
        {
            Source = TranslationService.Source,
            Path = new PropertyPath($"[{key}]"),
            Mode = BindingMode.OneWay,
            FallbackValue = FallbackValue,
            Converter = value is null && StringFormat is null ? null : FormatValueConverter.Instance,
            ConverterParameter = new FormatValueConverterParameter(key, value, StringFormat)
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

internal sealed record FormatValueConverterParameter(string Key, object? Value, string? StringFormat);

internal sealed class FormatValueConverter : IValueConverter
{
    public static readonly FormatValueConverter Instance = new();

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is not FormatValueConverterParameter formatParameter)
        {
            return value;
        }

        object? result = formatParameter.Value is null
            ? TranslationService.Source.Translate(formatParameter.Key)
            : TranslationService.Source.Translate(formatParameter.Key, formatParameter.Value);

        return formatParameter.StringFormat is null
            ? result
            : string.Format(TranslationService.Source.TranslationService.CurrentCulture, formatParameter.StringFormat, result);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
