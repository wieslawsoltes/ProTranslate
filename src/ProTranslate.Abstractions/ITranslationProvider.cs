using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Provides localized strings for a specific culture.
/// </summary>
public interface ITranslationProvider
{
    /// <summary>
    /// Gets the display name of the provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Looks up a localized string for the supplied key and culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The culture to use for the lookup.</param>
    /// <returns>The localized lookup result.</returns>
    LocalizedString GetString(string key, CultureInfo culture);
}
