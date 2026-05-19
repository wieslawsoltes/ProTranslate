namespace ProTranslate;

/// <summary>
/// Describes the units preferred by a measurement system profile.
/// </summary>
/// <param name="System">The measurement system.</param>
/// <param name="LengthUnit">The default length unit.</param>
/// <param name="TemperatureUnit">The default temperature unit.</param>
/// <param name="MassUnit">The default mass unit.</param>
/// <param name="VolumeUnit">The default volume unit.</param>
/// <param name="SpeedUnit">The default speed unit.</param>
public sealed record MeasurementSystemProfile(
    MeasurementSystem System,
    string LengthUnit,
    string TemperatureUnit,
    string MassUnit,
    string VolumeUnit,
    string SpeedUnit);
