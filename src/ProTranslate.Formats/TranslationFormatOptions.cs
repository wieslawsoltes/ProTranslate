namespace ProTranslate.Formats;

public sealed class TranslationFormatOptions
{
    public string? Culture { get; set; }

    public string? SourceCulture { get; set; }

    public string? Name { get; set; }

    public bool IncludeComments { get; set; } = true;

    public char Delimiter { get; set; } = ',';
}
