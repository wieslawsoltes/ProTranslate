namespace ProTranslate;

/// <summary>
/// Default framework-neutral measurement system resolver.
/// </summary>
public sealed class DefaultMeasurementSystemResolver : IMeasurementSystemResolver
{
    private static readonly MeasurementSystemProfile MetricProfile = new(
        MeasurementSystem.Metric,
        "m",
        "C",
        "kg",
        "l",
        "km/h");

    private static readonly MeasurementSystemProfile USCustomaryProfile = new(
        MeasurementSystem.USCustomary,
        "ft",
        "F",
        "lb",
        "gal",
        "mph");

    private static readonly MeasurementSystemProfile ImperialProfile = new(
        MeasurementSystem.Imperial,
        "ft",
        "C",
        "st",
        "imp gal",
        "mph");

    private static readonly MeasurementSystemProfile CustomProfile = new(
        MeasurementSystem.Custom,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);

    /// <inheritdoc />
    public MeasurementSystemProfile Resolve(
        RegionProfile regionProfile,
        MeasurementSystem? measurementSystemOverride = null)
    {
        ArgumentNullException.ThrowIfNull(regionProfile);

        MeasurementSystem system = measurementSystemOverride ?? ResolveDefault(regionProfile);
        return system switch
        {
            MeasurementSystem.Metric => MetricProfile,
            MeasurementSystem.USCustomary => USCustomaryProfile,
            MeasurementSystem.Imperial => ImperialProfile,
            _ => CustomProfile
        };
    }

    private static MeasurementSystem ResolveDefault(RegionProfile regionProfile) =>
        regionProfile.TwoLetterISORegionName switch
        {
            "US" or "LR" or "MM" => MeasurementSystem.USCustomary,
            "GB" => MeasurementSystem.Imperial,
            _ when regionProfile.IsMetric => MeasurementSystem.Metric,
            _ => MeasurementSystem.Custom
        };
}
