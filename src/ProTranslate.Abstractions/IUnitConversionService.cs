namespace ProTranslate;

/// <summary>
/// Converts values between supported measurement units.
/// </summary>
public interface IUnitConversionService
{
    /// <summary>
    /// Gets the category for a supported unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The unit category.</returns>
    MeasurementUnitCategory GetCategory(MeasurementUnit unit);

    /// <summary>
    /// Converts a value between compatible units.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="fromUnit">The source unit.</param>
    /// <param name="toUnit">The target unit.</param>
    /// <returns>The converted value.</returns>
    double Convert(double value, MeasurementUnit fromUnit, MeasurementUnit toUnit);

    /// <summary>
    /// Converts a value to the default unit for its category in a measurement profile.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="fromUnit">The source unit.</param>
    /// <param name="profile">The target measurement system profile.</param>
    /// <returns>The converted measurement value.</returns>
    MeasurementValue ConvertToProfile(double value, MeasurementUnit fromUnit, MeasurementSystemProfile profile);
}
