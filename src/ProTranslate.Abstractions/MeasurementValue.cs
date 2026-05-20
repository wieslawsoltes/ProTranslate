namespace ProTranslate;

/// <summary>
/// Represents a numeric measurement value and its unit.
/// </summary>
/// <param name="Value">The numeric value.</param>
/// <param name="Unit">The unit for the value.</param>
public readonly record struct MeasurementValue(double Value, MeasurementUnit Unit);
