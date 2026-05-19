namespace ProTranslate.Formats;

public sealed class TranslationCatalogEntry
{
    public TranslationCatalogEntry(string key, string? culture, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        Key = key;
        Culture = string.IsNullOrWhiteSpace(culture) ? null : culture;
        Value = value;
    }

    public string Key { get; set; }

    public string? Culture { get; set; }

    public string Value { get; set; }

    public string? Source { get; set; }

    public string? Comment { get; set; }

    public string? Context { get; set; }

    public string? State { get; set; }

    public string? PluralCategory { get; set; }

    public string? Format { get; set; }

    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);
}
