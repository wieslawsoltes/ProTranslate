namespace ProTranslate;

/// <summary>
/// Describes the measurement system preferred for a region or user profile.
/// </summary>
public enum MeasurementSystem
{
    /// <summary>
    /// Metric units such as kilometres, kilograms, and Celsius.
    /// </summary>
    Metric,

    /// <summary>
    /// US customary units such as miles, pounds, and Fahrenheit.
    /// </summary>
    USCustomary,

    /// <summary>
    /// Imperial-compatible units used by applications that distinguish them from US customary units.
    /// </summary>
    Imperial,

    /// <summary>
    /// Application-defined unit policy.
    /// </summary>
    Custom
}
