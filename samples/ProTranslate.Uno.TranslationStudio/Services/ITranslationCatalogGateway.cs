namespace ProTranslate.Uno.TranslationStudio;

public interface ITranslationCatalogGateway
{
    TranslationCatalogSnapshot LoadDemoCatalog(CatalogFormatChoice sourceFormat, CultureChoice targetCulture);

    TranslationCatalogSnapshot ImportCatalog(string content, CatalogFormatChoice sourceFormat, CultureChoice targetCulture);

    string CreateExportPreview(TranslationCatalogSnapshot snapshot, CatalogFormatChoice exportFormat);

    string ExportCatalog(TranslationCatalogSnapshot snapshot, CatalogFormatChoice exportFormat);

    /// <summary>
    /// Imports a translation catalog from a file on disk.
    /// </summary>
    Task<TranslationCatalogSnapshot> ImportFromFileAsync(
        string filePath,
        CatalogFormatChoice sourceFormat,
        CultureChoice targetCulture);

    /// <summary>
    /// Exports a translation catalog to a file on disk.
    /// </summary>
    Task ExportToFileAsync(
        TranslationCatalogSnapshot snapshot,
        string filePath,
        CatalogFormatChoice exportFormat);
}
