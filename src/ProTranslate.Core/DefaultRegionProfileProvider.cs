using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Default framework-neutral region profile provider.
/// </summary>
public sealed class DefaultRegionProfileProvider : IRegionProfileProvider
{
    /// <inheritdoc />
    public RegionProfile GetProfile(CultureInfo culture, RegionInfo? regionOverride = null)
    {
        ArgumentNullException.ThrowIfNull(culture);

        RegionInfo region = regionOverride ?? CreateRegion(culture);
        return new RegionProfile(region, culture);
    }

    private static RegionInfo CreateRegion(CultureInfo culture)
    {
        if (string.IsNullOrEmpty(culture.Name))
        {
            return RegionInfo.CurrentRegion;
        }

        CultureInfo specificCulture = culture.IsNeutralCulture
            ? CultureInfo.CreateSpecificCulture(culture.Name)
            : culture;

        return new RegionInfo(specificCulture.Name);
    }
}
