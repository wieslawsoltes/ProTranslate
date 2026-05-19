namespace ProTranslate;

/// <summary>
/// Controls how <see cref="ICultureService"/> applies culture changes outside its own snapshot.
/// </summary>
public sealed class CultureServiceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether culture changes should update
    /// <see cref="System.Globalization.CultureInfo.DefaultThreadCurrentCulture"/> and
    /// <see cref="System.Globalization.CultureInfo.DefaultThreadCurrentUICulture"/>.
    /// </summary>
    public bool ApplyToDefaultThread { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether culture changes should update
    /// <see cref="System.Threading.Thread.CurrentThread"/> culture state.
    /// </summary>
    public bool ApplyToCurrentThread { get; set; } = true;
}
