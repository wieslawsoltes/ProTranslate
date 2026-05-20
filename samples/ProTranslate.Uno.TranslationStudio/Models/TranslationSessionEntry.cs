namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Flat, JSON-serializable representation of a single translation entry for session persistence.
/// </summary>
public sealed record TranslationSessionEntry(
    string Key,
    string SourceText,
    string TargetText,
    string State,
    string Notes,
    string Diagnostics);
