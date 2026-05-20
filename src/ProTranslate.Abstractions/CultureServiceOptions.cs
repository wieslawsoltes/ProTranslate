namespace ProTranslate;

/// <summary>
/// Controls how <see cref="ICultureService"/> applies explicit culture changes outside its own snapshot.
/// </summary>
public sealed class CultureServiceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ICultureService.SetCulture(System.Globalization.CultureInfo)"/>
    /// should update
    /// <see cref="System.Globalization.CultureInfo.DefaultThreadCurrentCulture"/> and
    /// <see cref="System.Globalization.CultureInfo.DefaultThreadCurrentUICulture"/>.
    /// </summary>
    public bool ApplyToDefaultThread { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ICultureService.SetCulture(System.Globalization.CultureInfo)"/>
    /// should update
    /// <see cref="System.Threading.Thread.CurrentThread"/> culture state.
    /// </summary>
    public bool ApplyToCurrentThread { get; set; } = true;
}
