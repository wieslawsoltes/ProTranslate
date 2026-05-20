using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Describes immutable region metadata for one culture snapshot.
/// </summary>
/// <param name="Region">The underlying .NET region.</param>
/// <param name="Culture">The culture used to resolve the region.</param>
public sealed record RegionProfile(RegionInfo Region, CultureInfo Culture)
{
    /// <summary>
    /// Gets the two-letter ISO region name.
    /// </summary>
    public string TwoLetterISORegionName => Region.TwoLetterISORegionName;

    /// <summary>
    /// Gets the English region name.
    /// </summary>
    public string EnglishName => Region.EnglishName;

    /// <summary>
    /// Gets the native region name.
    /// </summary>
    public string NativeName => Region.NativeName;

    /// <summary>
    /// Gets the ISO currency symbol.
    /// </summary>
    public string ISOCurrencySymbol => Region.ISOCurrencySymbol;

    /// <summary>
    /// Gets the currency symbol.
    /// </summary>
    public string CurrencySymbol => Region.CurrencySymbol;

    /// <summary>
    /// Gets a value indicating whether the region defaults to metric units.
    /// </summary>
    public bool IsMetric => Region.IsMetric;
}
