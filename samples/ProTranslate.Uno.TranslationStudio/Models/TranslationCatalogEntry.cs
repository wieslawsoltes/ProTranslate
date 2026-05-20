namespace ProTranslate.Uno.TranslationStudio;

public sealed record TranslationCatalogEntry(
    string Key,
    string SourceText,
    string TargetText,
    TranslationReviewState State,
    string Notes,
    string Diagnostics);
