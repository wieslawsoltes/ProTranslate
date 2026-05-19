namespace ProTranslate.Uno.TranslationStudio;

public interface ITranslationCatalogGateway
{
    TranslationCatalogSnapshot LoadDemoCatalog(CatalogFormatChoice sourceFormat, CultureChoice targetCulture);

    string CreateExportPreview(TranslationCatalogSnapshot snapshot, CatalogFormatChoice exportFormat);
}
