using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Resolves immutable region profiles for cultures and explicit region overrides.
/// </summary>
public interface IRegionProfileProvider
{
    /// <summary>
    /// Gets a region profile for the supplied culture and optional explicit region override.
    /// </summary>
    /// <param name="culture">The formatting culture.</param>
    /// <param name="regionOverride">The optional explicit region override.</param>
    /// <returns>The resolved region profile.</returns>
    RegionProfile GetProfile(CultureInfo culture, RegionInfo? regionOverride = null);
}
