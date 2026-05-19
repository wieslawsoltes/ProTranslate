using System.Globalization;

namespace ProTranslate.Formats;

public sealed class TranslationCatalog
{
    private readonly List<TranslationCatalogEntry> _entries = [];

    public string? Name { get; set; }

    public string? SourceCulture { get; set; }

    public IList<TranslationCatalogEntry> Entries => _entries;

    public IReadOnlyList<string> Cultures => _entries
        .Select(static entry => entry.Culture)
        .Where(static culture => !string.IsNullOrWhiteSpace(culture))
        .Select(static culture => culture!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(static culture => culture, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public TranslationCatalogEntry Add(string key, string? culture, string value)
    {
        var entry = new TranslationCatalogEntry(key, culture, value);
        _entries.Add(entry);
        return entry;
    }

    public IReadOnlyDictionary<string, string> ToDictionary(string? culture)
    {
        return _entries
            .Where(entry => string.Equals(entry.Culture, culture, StringComparison.OrdinalIgnoreCase))
            .GroupBy(static entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.Last().Value, StringComparer.Ordinal);
    }

    public static string NormalizeCultureName(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return string.Empty;
        }

        try
        {
            return CultureInfo.GetCultureInfo(culture).Name;
        }
        catch (CultureNotFoundException)
        {
            return culture.Trim();
        }
    }
}
