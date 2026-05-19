using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Describes a structured ProTranslate diagnostic.
/// </summary>
/// <param name="Kind">The diagnostic kind.</param>
/// <param name="Severity">The diagnostic severity.</param>
/// <param name="Message">The diagnostic message.</param>
/// <param name="Key">The resource key related to the diagnostic.</param>
/// <param name="Culture">The culture related to the diagnostic.</param>
/// <param name="ProviderName">The provider related to the diagnostic.</param>
/// <param name="Exception">The exception related to the diagnostic.</param>
public sealed record ProTranslateDiagnostic(
    ProTranslateDiagnosticKind Kind,
    ProTranslateDiagnosticSeverity Severity,
    string Message,
    string? Key = null,
    CultureInfo? Culture = null,
    string? ProviderName = null,
    Exception? Exception = null);
