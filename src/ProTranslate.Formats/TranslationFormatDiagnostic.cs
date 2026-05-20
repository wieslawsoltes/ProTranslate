namespace ProTranslate.Formats;

public enum TranslationFormatDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record TranslationFormatDiagnostic(
    TranslationFormatDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Key = null,
    string? Culture = null);
