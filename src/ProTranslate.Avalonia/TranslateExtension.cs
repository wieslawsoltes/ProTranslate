using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Data;

namespace ProTranslate.Avalonia;

public class TranslateExtension
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

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var key = Key ?? string.Empty;

        return new Binding
        {
            Source = TranslationService.Source,
            Path = $"[{key}]",
            Mode = BindingMode.OneWay,
            FallbackValue = FallbackValue,
            StringFormat = StringFormat
        };
    }
}

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

public class FormatExtension
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

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var key = Key ?? string.Empty;

        if (Value is null)
        {
            return new Binding
            {
                Source = TranslationService.Source,
                Path = $"[{key}]",
                Mode = BindingMode.OneWay,
                FallbackValue = FallbackValue,
                StringFormat = StringFormat
            };
        }

        var binding = new MultiBinding
        {
            Converter = FormatValueConverter.Instance,
            ConverterParameter = new FormatValueConverterParameter(key, StringFormat)
        };

        if (FallbackValue is not null)
        {
            binding.FallbackValue = FallbackValue;
        }

        binding.Bindings.Add(new Binding
        {
            Source = TranslationService.Source,
            Path = $"[{key}]",
            Mode = BindingMode.OneWay
        });
        binding.Bindings.Add(Value is BindingBase valueBinding ? valueBinding : new Binding
        {
            Source = new ConstantValueSource(Value),
            Path = nameof(ConstantValueSource.Value),
            Mode = BindingMode.OneWay
        });

        return binding;
    }

    private sealed class ConstantValueSource(object? value)
    {
        public object? Value { get; } = value;
    }

    private sealed record FormatValueConverterParameter(string Key, string? StringFormat);

    private sealed class FormatValueConverter : IMultiValueConverter
    {
        public static readonly FormatValueConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is not FormatValueConverterParameter formatParameter)
            {
                return AvaloniaProperty.UnsetValue;
            }

            if (values.Count > 0 && IsUnset(values[0]))
            {
                return AvaloniaProperty.UnsetValue;
            }

            if (values.Count > 1 && IsUnset(values[1]))
            {
                return AvaloniaProperty.UnsetValue;
            }

            object? result = values.Count <= 1
                ? TranslationService.Source.Translate(formatParameter.Key)
                : TranslationService.Source.Translate(formatParameter.Key, values.Skip(1).ToArray());

            return formatParameter.StringFormat is null
                ? result
                : string.Format(culture, formatParameter.StringFormat, result);
        }

        private static bool IsUnset(object? value)
        {
            return ReferenceEquals(value, AvaloniaProperty.UnsetValue) ||
                   ReferenceEquals(value, BindingOperations.DoNothing);
        }
    }
}

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
