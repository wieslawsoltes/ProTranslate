namespace ProTranslate.Formats;

public sealed class TranslationFormatResult
{
    public TranslationFormatResult(TranslationCatalog catalog, IReadOnlyList<TranslationFormatDiagnostic> diagnostics)
    {
        Catalog = catalog;
        Diagnostics = diagnostics;
    }

    public TranslationCatalog Catalog { get; }

    public IReadOnlyList<TranslationFormatDiagnostic> Diagnostics { get; }
}
