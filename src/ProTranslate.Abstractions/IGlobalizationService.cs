using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Combines culture management and translation lookup behind one application service.
/// </summary>
public interface IGlobalizationService
{
    /// <summary>
    /// Occurs when the active culture changes.
    /// </summary>
    event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <summary>
    /// Gets the culture service.
    /// </summary>
    ICultureService Cultures { get; }

    /// <summary>
    /// Gets the translation service.
    /// </summary>
    ITranslationService Translations { get; }

    /// <summary>
    /// Gets the unit conversion service.
    /// </summary>
    IUnitConversionService UnitConverter { get; }

    /// <summary>
    /// Gets the localized unit formatter.
    /// </summary>
    ILocalizedUnitFormatter UnitFormatter { get; }

    /// <summary>
    /// Gets the active culture.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets a value indicating whether the active region uses the metric system.
    /// </summary>
    bool IsMetric { get; }

    /// <summary>
    /// Gets the active region profile.
    /// </summary>
    RegionProfile RegionProfile { get; }

    /// <summary>
    /// Gets the preferred measurement system for the active region.
    /// </summary>
    MeasurementSystem MeasurementSystem { get; }

    /// <summary>
    /// Gets the active measurement system profile.
    /// </summary>
    MeasurementSystemProfile MeasurementSystemProfile { get; }

    /// <summary>
    /// Gets the natural text flow direction for the active culture.
    /// </summary>
    TextFlowDirection FlowDirection { get; }

    /// <summary>
    /// Sets the active culture.
    /// </summary>
    /// <param name="culture">The culture to activate.</param>
    void SetCulture(CultureInfo culture);

    /// <summary>
    /// Sets the explicit region override.
    /// </summary>
    /// <param name="region">The region override.</param>
    void SetRegionOverride(RegionInfo region);

    /// <summary>
    /// Clears the explicit region override and returns to culture-derived region resolution.
    /// </summary>
    void ClearRegionOverride();

    /// <summary>
    /// Sets the explicit measurement system override.
    /// </summary>
    /// <param name="measurementSystem">The measurement system override.</param>
    void SetMeasurementSystemOverride(MeasurementSystem measurementSystem);

    /// <summary>
    /// Clears the explicit measurement system override and returns to region-derived measurement resolution.
    /// </summary>
    void ClearMeasurementSystemOverride();

    /// <summary>
    /// Gets a localized string for the active culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized lookup result.</returns>
    LocalizedString GetString(string key);

    /// <summary>
    /// Converts a measurement value to the active measurement system profile.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="sourceUnit">The source unit.</param>
    /// <returns>The converted measurement value.</returns>
    MeasurementValue ConvertMeasurement(double value, MeasurementUnit sourceUnit);

    /// <summary>
    /// Formats a measurement value using the active culture.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="unit">The unit.</param>
    /// <param name="format">The optional numeric format string.</param>
    /// <returns>The localized measurement text.</returns>
    string FormatMeasurement(double value, MeasurementUnit unit, string? format = null);

    /// <summary>
    /// Converts a measurement value to the active measurement system profile and formats it using the active culture.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="sourceUnit">The source unit.</param>
    /// <param name="format">The optional numeric format string.</param>
    /// <returns>The localized measurement text.</returns>
    string FormatMeasurementForProfile(double value, MeasurementUnit sourceUnit, string? format = null);
}
