using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Default formatter for localized numeric measurement values.
/// </summary>
public sealed class DefaultLocalizedUnitFormatter : ILocalizedUnitFormatter
{
    /// <inheritdoc />
    public string Format(double value, MeasurementUnit unit, CultureInfo culture, string? format = null)
    {
        ArgumentNullException.ThrowIfNull(culture);

        string number = value.ToString(format ?? "G", culture);
        return string.Concat(number, " ", GetSymbol(unit));
    }

    /// <inheritdoc />
    public string Format(MeasurementValue value, CultureInfo culture, string? format = null) =>
        Format(value.Value, value.Unit, culture, format);

    private static string GetSymbol(MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.Meter => "m",
            MeasurementUnit.Kilometer => "km",
            MeasurementUnit.Centimeter => "cm",
            MeasurementUnit.Millimeter => "mm",
            MeasurementUnit.Inch => "in",
            MeasurementUnit.Foot => "ft",
            MeasurementUnit.Yard => "yd",
            MeasurementUnit.Mile => "mi",
            MeasurementUnit.Celsius => "°C",
            MeasurementUnit.Fahrenheit => "°F",
            MeasurementUnit.Kelvin => "K",
            MeasurementUnit.Gram => "g",
            MeasurementUnit.Kilogram => "kg",
            MeasurementUnit.Ounce => "oz",
            MeasurementUnit.Pound => "lb",
            MeasurementUnit.Stone => "st",
            MeasurementUnit.Milliliter => "mL",
            MeasurementUnit.Liter => "L",
            MeasurementUnit.USFluidOunce => "us fl oz",
            MeasurementUnit.USCup => "cup",
            MeasurementUnit.USPint => "us pt",
            MeasurementUnit.USQuart => "us qt",
            MeasurementUnit.USGallon => "gal",
            MeasurementUnit.ImperialFluidOunce => "imp fl oz",
            MeasurementUnit.ImperialPint => "imp pt",
            MeasurementUnit.ImperialQuart => "imp qt",
            MeasurementUnit.ImperialGallon => "imp gal",
            MeasurementUnit.MetersPerSecond => "m/s",
            MeasurementUnit.KilometersPerHour => "km/h",
            MeasurementUnit.MilesPerHour => "mph",
            MeasurementUnit.FeetPerSecond => "ft/s",
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported measurement unit.")
        };
}
