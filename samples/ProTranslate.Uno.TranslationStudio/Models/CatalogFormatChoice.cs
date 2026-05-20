using ProTranslate.Formats;

namespace ProTranslate.Uno.TranslationStudio;

public sealed record CatalogFormatChoice(string Name, string Extension, string Description, TranslationFileFormat Format);
