namespace ProTranslate.Uno.TranslationStudio;

public sealed record TranslationCatalogSnapshot(
    string FileName,
    string SourceCulture,
    string TargetCulture,
    string SourceFormat,
    IReadOnlyList<TranslationCatalogEntry> Entries,
    IReadOnlyList<TranslationCoverageColumn> Coverage);
