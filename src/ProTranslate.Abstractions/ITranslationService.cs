using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Resolves localized strings for the active culture.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Occurs when the active culture changes.
    /// </summary>
    event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <summary>
    /// Occurs when lookup or formatting produces a structured diagnostic.
    /// </summary>
    event EventHandler<ProTranslateDiagnosticEventArgs>? DiagnosticReported;

    /// <summary>
    /// Gets the active culture.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets the active UI culture used for translation lookup.
    /// </summary>
    CultureInfo CurrentUICulture { get; }

    /// <summary>
    /// Gets a localized string for the active culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized lookup result.</returns>
    LocalizedString this[string key] { get; }

    /// <summary>
    /// Gets a localized string for the active culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized lookup result.</returns>
    LocalizedString GetString(string key);

    /// <summary>
    /// Gets a localized string for a specific culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The culture to use for the lookup.</param>
    /// <returns>The localized lookup result.</returns>
    LocalizedString GetString(string key, CultureInfo culture);

    /// <summary>
    /// Formats a localized string for the active culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="arguments">The format arguments.</param>
    /// <returns>The formatted localized value.</returns>
    string Format(string key, params object?[] arguments);

    /// <summary>
    /// Creates an observable localized string suitable for compiled bindings.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="arguments">The optional format arguments.</param>
    /// <returns>An observable localized string proxy.</returns>
    IObservableLocalizedString Observe(string key, params object?[] arguments);
}
