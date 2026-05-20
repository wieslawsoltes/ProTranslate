namespace ProTranslate;

/// <summary>
/// Default unit conversion service for built-in measurement units.
/// </summary>
public sealed class DefaultUnitConversionService : IUnitConversionService
{
    /// <inheritdoc />
    public MeasurementUnitCategory GetCategory(MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.Meter or
            MeasurementUnit.Kilometer or
            MeasurementUnit.Centimeter or
            MeasurementUnit.Millimeter or
            MeasurementUnit.Inch or
            MeasurementUnit.Foot or
            MeasurementUnit.Yard or
            MeasurementUnit.Mile => MeasurementUnitCategory.Length,

            MeasurementUnit.Celsius or
            MeasurementUnit.Fahrenheit or
            MeasurementUnit.Kelvin => MeasurementUnitCategory.Temperature,

            MeasurementUnit.Gram or
            MeasurementUnit.Kilogram or
            MeasurementUnit.Ounce or
            MeasurementUnit.Pound or
            MeasurementUnit.Stone => MeasurementUnitCategory.Mass,

            MeasurementUnit.Milliliter or
            MeasurementUnit.Liter or
            MeasurementUnit.USFluidOunce or
            MeasurementUnit.USCup or
            MeasurementUnit.USPint or
            MeasurementUnit.USQuart or
            MeasurementUnit.USGallon or
            MeasurementUnit.ImperialFluidOunce or
            MeasurementUnit.ImperialPint or
            MeasurementUnit.ImperialQuart or
            MeasurementUnit.ImperialGallon => MeasurementUnitCategory.Volume,

            MeasurementUnit.MetersPerSecond or
            MeasurementUnit.KilometersPerHour or
            MeasurementUnit.MilesPerHour or
            MeasurementUnit.FeetPerSecond => MeasurementUnitCategory.Speed,

            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported measurement unit.")
        };

    /// <inheritdoc />
    public double Convert(double value, MeasurementUnit fromUnit, MeasurementUnit toUnit)
    {
        MeasurementUnitCategory fromCategory = GetCategory(fromUnit);
        MeasurementUnitCategory toCategory = GetCategory(toUnit);
        if (fromCategory != toCategory)
        {
            throw new ArgumentException("Units must belong to the same measurement category.", nameof(toUnit));
        }

        if (fromUnit == toUnit)
        {
            return value;
        }

        return fromCategory switch
        {
            MeasurementUnitCategory.Length => FromMeters(ToMeters(value, fromUnit), toUnit),
            MeasurementUnitCategory.Temperature => FromCelsius(ToCelsius(value, fromUnit), toUnit),
            MeasurementUnitCategory.Mass => FromKilograms(ToKilograms(value, fromUnit), toUnit),
            MeasurementUnitCategory.Volume => FromLiters(ToLiters(value, fromUnit), toUnit),
            MeasurementUnitCategory.Speed => FromMetersPerSecond(ToMetersPerSecond(value, fromUnit), toUnit),
            _ => throw new ArgumentOutOfRangeException(nameof(fromUnit), fromUnit, "Unsupported measurement unit.")
        };
    }

    /// <inheritdoc />
    public MeasurementValue ConvertToProfile(double value, MeasurementUnit fromUnit, MeasurementSystemProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        MeasurementUnit targetUnit = ResolveProfileUnit(GetCategory(fromUnit), profile);
        return new MeasurementValue(Convert(value, fromUnit, targetUnit), targetUnit);
    }

    private static double ToMeters(double value, MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.Meter => value,
            MeasurementUnit.Kilometer => value * 1000d,
            MeasurementUnit.Centimeter => value * 0.01d,
            MeasurementUnit.Millimeter => value * 0.001d,
            MeasurementUnit.Inch => value * 0.0254d,
            MeasurementUnit.Foot => value * 0.3048d,
            MeasurementUnit.Yard => value * 0.9144d,
            MeasurementUnit.Mile => value * 1609.344d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported length unit.")
        };

    private static double FromMeters(double value, MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.Meter => value,
            MeasurementUnit.Kilometer => value / 1000d,
            MeasurementUnit.Centimeter => value / 0.01d,
            MeasurementUnit.Millimeter => value / 0.001d,
            MeasurementUnit.Inch => value / 0.0254d,
            MeasurementUnit.Foot => value / 0.3048d,
            MeasurementUnit.Yard => value / 0.9144d,
            MeasurementUnit.Mile => value / 1609.344d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported length unit.")
        };

    private static double ToCelsius(double value, MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.Celsius => value,
            MeasurementUnit.Fahrenheit => (value - 32d) * 5d / 9d,
            MeasurementUnit.Kelvin => value - 273.15d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported temperature unit.")
        };

    private static double FromCelsius(double value, MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.Celsius => value,
            MeasurementUnit.Fahrenheit => (value * 9d / 5d) + 32d,
            MeasurementUnit.Kelvin => value + 273.15d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported temperature unit.")
        };

    private static double ToKilograms(double value, MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.Gram => value * 0.001d,
            MeasurementUnit.Kilogram => value,
            MeasurementUnit.Ounce => value * 0.028349523125d,
            MeasurementUnit.Pound => value * 0.45359237d,
            MeasurementUnit.Stone => value * 6.35029318d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported mass unit.")
        };

    private static double FromKilograms(double value, MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.Gram => value / 0.001d,
            MeasurementUnit.Kilogram => value,
            MeasurementUnit.Ounce => value / 0.028349523125d,
            MeasurementUnit.Pound => value / 0.45359237d,
            MeasurementUnit.Stone => value / 6.35029318d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported mass unit.")
        };

    private static double ToLiters(double value, MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.Milliliter => value * 0.001d,
            MeasurementUnit.Liter => value,
            MeasurementUnit.USFluidOunce => value * 0.0295735295625d,
            MeasurementUnit.USCup => value * 0.2365882365d,
            MeasurementUnit.USPint => value * 0.473176473d,
            MeasurementUnit.USQuart => value * 0.946352946d,
            MeasurementUnit.USGallon => value * 3.785411784d,
            MeasurementUnit.ImperialFluidOunce => value * 0.0284130625d,
            MeasurementUnit.ImperialPint => value * 0.56826125d,
            MeasurementUnit.ImperialQuart => value * 1.1365225d,
            MeasurementUnit.ImperialGallon => value * 4.54609d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported volume unit.")
        };

    private static double FromLiters(double value, MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.Milliliter => value / 0.001d,
            MeasurementUnit.Liter => value,
            MeasurementUnit.USFluidOunce => value / 0.0295735295625d,
            MeasurementUnit.USCup => value / 0.2365882365d,
            MeasurementUnit.USPint => value / 0.473176473d,
            MeasurementUnit.USQuart => value / 0.946352946d,
            MeasurementUnit.USGallon => value / 3.785411784d,
            MeasurementUnit.ImperialFluidOunce => value / 0.0284130625d,
            MeasurementUnit.ImperialPint => value / 0.56826125d,
            MeasurementUnit.ImperialQuart => value / 1.1365225d,
            MeasurementUnit.ImperialGallon => value / 4.54609d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported volume unit.")
        };

    private static double ToMetersPerSecond(double value, MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.MetersPerSecond => value,
            MeasurementUnit.KilometersPerHour => value * 1000d / 3600d,
            MeasurementUnit.MilesPerHour => value * 1609.344d / 3600d,
            MeasurementUnit.FeetPerSecond => value * 0.3048d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported speed unit.")
        };

    private static double FromMetersPerSecond(double value, MeasurementUnit unit) =>
        unit switch
        {
            MeasurementUnit.MetersPerSecond => value,
            MeasurementUnit.KilometersPerHour => value * 3600d / 1000d,
            MeasurementUnit.MilesPerHour => value * 3600d / 1609.344d,
            MeasurementUnit.FeetPerSecond => value / 0.3048d,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported speed unit.")
        };

    private static MeasurementUnit ResolveProfileUnit(
        MeasurementUnitCategory category,
        MeasurementSystemProfile profile)
    {
        string unit = category switch
        {
            MeasurementUnitCategory.Length => profile.LengthUnit,
            MeasurementUnitCategory.Temperature => profile.TemperatureUnit,
            MeasurementUnitCategory.Mass => profile.MassUnit,
            MeasurementUnitCategory.Volume => profile.VolumeUnit,
            MeasurementUnitCategory.Speed => profile.SpeedUnit,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported measurement category.")
        };

        if (TryResolveProfileUnit(unit, profile.System, out MeasurementUnit resolved))
        {
            return resolved;
        }

        throw new ArgumentException($"Measurement profile unit '{unit}' is not supported for conversion.", nameof(profile));
    }

    private static bool TryResolveProfileUnit(
        string unit,
        MeasurementSystem system,
        out MeasurementUnit measurementUnit)
    {
        measurementUnit = unit switch
        {
            "m" => MeasurementUnit.Meter,
            "km" => MeasurementUnit.Kilometer,
            "cm" => MeasurementUnit.Centimeter,
            "mm" => MeasurementUnit.Millimeter,
            "in" => MeasurementUnit.Inch,
            "ft" => MeasurementUnit.Foot,
            "yd" => MeasurementUnit.Yard,
            "mi" => MeasurementUnit.Mile,
            "C" or "°C" => MeasurementUnit.Celsius,
            "F" or "°F" => MeasurementUnit.Fahrenheit,
            "K" => MeasurementUnit.Kelvin,
            "g" => MeasurementUnit.Gram,
            "kg" => MeasurementUnit.Kilogram,
            "oz" => MeasurementUnit.Ounce,
            "lb" => MeasurementUnit.Pound,
            "st" => MeasurementUnit.Stone,
            "ml" or "mL" => MeasurementUnit.Milliliter,
            "l" or "L" => MeasurementUnit.Liter,
            "us fl oz" => MeasurementUnit.USFluidOunce,
            "cup" or "us cup" => MeasurementUnit.USCup,
            "pt" or "us pt" => MeasurementUnit.USPint,
            "qt" or "us qt" => MeasurementUnit.USQuart,
            "gal" when system == MeasurementSystem.Imperial => MeasurementUnit.ImperialGallon,
            "gal" => MeasurementUnit.USGallon,
            "imp fl oz" => MeasurementUnit.ImperialFluidOunce,
            "imp pt" => MeasurementUnit.ImperialPint,
            "imp qt" => MeasurementUnit.ImperialQuart,
            "imp gal" => MeasurementUnit.ImperialGallon,
            "m/s" => MeasurementUnit.MetersPerSecond,
            "km/h" => MeasurementUnit.KilometersPerHour,
            "mph" => MeasurementUnit.MilesPerHour,
            "ft/s" => MeasurementUnit.FeetPerSecond,
            _ => default
        };

        return measurementUnit != default || unit == "m";
    }
}
