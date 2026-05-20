namespace ProTranslate;

/// <summary>
/// Resolves measurement system profiles from region policy and optional user preference.
/// </summary>
public interface IMeasurementSystemResolver
{
    /// <summary>
    /// Resolves a measurement system profile.
    /// </summary>
    /// <param name="regionProfile">The active region profile.</param>
    /// <param name="measurementSystemOverride">The optional explicit measurement system override.</param>
    /// <returns>The resolved measurement system profile.</returns>
    MeasurementSystemProfile Resolve(
        RegionProfile regionProfile,
        MeasurementSystem? measurementSystemOverride = null);
}
