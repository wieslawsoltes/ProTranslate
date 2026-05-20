using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Represents a localized value returned by a translation provider.
/// </summary>
/// <param name="Key">The resource key that was requested.</param>
/// <param name="Value">The localized value or fallback value.</param>
/// <param name="Culture">The culture used for the lookup.</param>
/// <param name="ResourceNotFound">A value indicating whether the lookup missed every resource source.</param>
/// <param name="ProviderName">The provider that supplied the value.</param>
public readonly record struct LocalizedString(
    string Key,
    string Value,
    CultureInfo Culture,
    bool ResourceNotFound,
    string? ProviderName)
{
    /// <summary>
    /// Gets diagnostics collected while resolving this value.
    /// </summary>
    public IReadOnlyList<ProTranslateDiagnostic> Diagnostics { get; init; } = Array.Empty<ProTranslateDiagnostic>();

    /// <summary>
    /// Returns the localized value.
    /// </summary>
    /// <returns>The localized value.</returns>
    public override string ToString() => Value;
}
