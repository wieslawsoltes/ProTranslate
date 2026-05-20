namespace ProTranslate;

/// <summary>
/// Controls translation lookup caching.
/// </summary>
public sealed class TranslationCacheOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether provider lookup results are cached.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether missing translation results are cached.
    /// </summary>
    public bool CacheMissingTranslations { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the cache is cleared when the active culture snapshot changes.
    /// </summary>
    public bool ClearOnCultureChanged { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of cached lookup results. A value less than one disables the limit.
    /// </summary>
    public int MaximumEntries { get; set; }
}
