namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Request model for AI-powered translation of one or more entries.
/// </summary>
public sealed record AiTranslationRequest(
    string SourceLanguage,
    string TargetLanguage,
    IReadOnlyList<AiTranslationRequestEntry> Entries,
    string Context = "");

/// <summary>
/// A single entry within an AI translation request.
/// </summary>
public sealed record AiTranslationRequestEntry(
    string Key,
    string SourceText);
