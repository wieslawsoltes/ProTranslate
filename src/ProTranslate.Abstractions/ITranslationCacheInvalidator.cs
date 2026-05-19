using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Exposes cache invalidation for translation services that cache provider lookups.
/// </summary>
public interface ITranslationCacheInvalidator
{
    /// <summary>
    /// Gets the number of cached lookup results.
    /// </summary>
    int CachedLookupCount { get; }

    /// <summary>
    /// Removes all cached lookup results.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Removes cached lookup results for a key and requested culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The requested culture.</param>
    /// <returns><see langword="true"/> when at least one entry was removed.</returns>
    bool RemoveCachedString(string key, CultureInfo culture);
}
