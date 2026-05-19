using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Formats measurement values using culture-specific number formatting and unit symbols.
/// </summary>
public interface ILocalizedUnitFormatter
{
    /// <summary>
    /// Formats a measurement value.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="unit">The value unit.</param>
    /// <param name="culture">The formatting culture.</param>
    /// <param name="format">The optional numeric format string.</param>
    /// <returns>The localized measurement text.</returns>
    string Format(double value, MeasurementUnit unit, CultureInfo culture, string? format = null);

    /// <summary>
    /// Formats a measurement value.
    /// </summary>
    /// <param name="value">The measurement value.</param>
    /// <param name="culture">The formatting culture.</param>
    /// <param name="format">The optional numeric format string.</param>
    /// <returns>The localized measurement text.</returns>
    string Format(MeasurementValue value, CultureInfo culture, string? format = null);
}
