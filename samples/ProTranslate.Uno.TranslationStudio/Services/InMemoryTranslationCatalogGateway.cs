using ProTranslate.Formats;

namespace ProTranslate.Uno.TranslationStudio;

public sealed class InMemoryTranslationCatalogGateway : ITranslationCatalogGateway
{
    public TranslationCatalogSnapshot LoadDemoCatalog(CatalogFormatChoice sourceFormat, CultureChoice targetCulture)
    {
        ArgumentNullException.ThrowIfNull(sourceFormat);
        ArgumentNullException.ThrowIfNull(targetCulture);

        TranslationFormatResult imported = TranslationCatalogConverter.Import(
            CreateDemoCatalog(sourceFormat, targetCulture),
            sourceFormat.Format,
            new TranslationFormatOptions
            {
                Culture = targetCulture.CultureName,
                SourceCulture = "en-US",
                Name = "ProductCatalog"
            },
            $"ProductCatalog.{targetCulture.CultureName}{sourceFormat.Extension}");

        IReadOnlyList<TranslationCatalogEntry> entries = imported.Catalog.Entries
            .GroupBy(static entry => entry.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                ProTranslate.Formats.TranslationCatalogEntry importedEntry = group.Last();
                string source = importedEntry.Source ?? EnglishSource(importedEntry.Key);
                string diagnostics = importedEntry.Comment ?? string.Empty;
                TranslationReviewState state = string.IsNullOrWhiteSpace(importedEntry.Value)
                    ? TranslationReviewState.Missing
                    : diagnostics.Contains("review", StringComparison.OrdinalIgnoreCase)
                        ? TranslationReviewState.Review
                        : TranslationReviewState.Approved;

                return new TranslationCatalogEntry(
                    importedEntry.Key,
                    source,
                    importedEntry.Value,
                    state,
                    importedEntry.Comment ?? StateNote(state),
                    string.Join("; ", imported.Diagnostics.Select(static diagnostic => diagnostic.Message).Concat([diagnostics]).Where(static value => !string.IsNullOrWhiteSpace(value))));
            })
            .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
            .ToArray();

        IReadOnlyList<TranslationCoverageColumn> coverage =
        [
            new("en-US", "English", 1.0),
            new("pl-PL", "Polish", targetCulture.CultureName == "pl-PL" ? 0.88 : 0.79),
            new("de-DE", "German", targetCulture.CultureName == "de-DE" ? 0.91 : 0.72),
            new("ja-JP", "Japanese", targetCulture.CultureName == "ja-JP" ? 0.84 : 0.68)
        ];

        return new TranslationCatalogSnapshot(
            $"ProductCatalog.{targetCulture.CultureName}{sourceFormat.Extension}",
            "en-US",
            targetCulture.CultureName,
            sourceFormat.Name,
            entries,
            coverage);
    }

    public string CreateExportPreview(TranslationCatalogSnapshot snapshot, CatalogFormatChoice exportFormat)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(exportFormat);

        var catalog = new ProTranslate.Formats.TranslationCatalog
        {
            Name = Path.GetFileNameWithoutExtension(snapshot.FileName),
            SourceCulture = snapshot.SourceCulture
        };

        foreach (TranslationCatalogEntry entry in snapshot.Entries)
        {
            ProTranslate.Formats.TranslationCatalogEntry catalogEntry = catalog.Add(entry.Key, snapshot.TargetCulture, entry.TargetText);
            catalogEntry.Source = entry.SourceText;
            catalogEntry.Comment = entry.Notes;
            catalogEntry.State = entry.State.ToString();
        }

        string preview = TranslationCatalogConverter.Export(
            catalog,
            exportFormat.Format,
            new TranslationFormatOptions
            {
                Culture = snapshot.TargetCulture,
                SourceCulture = snapshot.SourceCulture,
                Name = catalog.Name
            });

        int readyEntries = snapshot.Entries.Count(entry => entry.State == TranslationReviewState.Approved);
        int previewBytes = System.Text.Encoding.UTF8.GetByteCount(preview);
        return $"{readyEntries}/{snapshot.Entries.Count} approved entries ready for {exportFormat.Name} export ({previewBytes:N0} bytes).";
    }

    private static string CreateDemoCatalog(CatalogFormatChoice sourceFormat, CultureChoice targetCulture)
    {
        var catalog = new ProTranslate.Formats.TranslationCatalog
        {
            Name = "ProductCatalog",
            SourceCulture = "en-US"
        };

        Add(catalog, "Shell.FileMenu", targetCulture.CultureName, "File", Translate(targetCulture.CultureName, "File"), TranslationReviewState.Approved, "Generated key constant expected.");
        Add(catalog, "Shell.Import", targetCulture.CultureName, "Import catalog", Translate(targetCulture.CultureName, "Import catalog"), TranslationReviewState.Approved, "Maps to toolbar action.");
        Add(catalog, "Shell.Export", targetCulture.CultureName, "Export catalog", Translate(targetCulture.CultureName, "Export catalog"), TranslationReviewState.Review, "Needs product owner review.");
        Add(catalog, "Orders.EmptyState", targetCulture.CultureName, "No orders require translation.", string.Empty, TranslationReviewState.Missing, "Missing target value.");
        Add(catalog, "Orders.Total", targetCulture.CultureName, "Total: {0}", Translate(targetCulture.CultureName, "Total: {0}"), TranslationReviewState.Approved, "One numeric placeholder.");
        Add(catalog, "Orders.DueDate", targetCulture.CultureName, "Due by {0:D}", Translate(targetCulture.CultureName, "Due by {0:D}"), TranslationReviewState.Review, "Verify date placeholder keeps the D format specifier.");
        Add(catalog, "Region.MeasurementSystem", targetCulture.CultureName, "Measurement system: {0}", Translate(targetCulture.CultureName, "Measurement system: {0}"), TranslationReviewState.Approved, "Region policy display.");
        Add(catalog, "Diagnostics.ProviderFailed", targetCulture.CultureName, "Provider {0} failed: {1}", Translate(targetCulture.CultureName, "Provider {0} failed: {1}"), TranslationReviewState.Review, "Two placeholders detected; provider name and message order preserved.");

        return TranslationCatalogConverter.Export(
            catalog,
            sourceFormat.Format,
            new TranslationFormatOptions
            {
                Culture = targetCulture.CultureName,
                SourceCulture = "en-US",
                Name = "ProductCatalog"
            });
    }

    private static void Add(
        ProTranslate.Formats.TranslationCatalog catalog,
        string key,
        string culture,
        string source,
        string target,
        TranslationReviewState state,
        string note)
    {
        ProTranslate.Formats.TranslationCatalogEntry entry = catalog.Add(key, culture, target);
        entry.Source = source;
        entry.Comment = note;
        entry.State = state.ToString();
    }

    private static string StateNote(TranslationReviewState state)
    {
        return state switch
        {
            TranslationReviewState.Approved => "Ready for export.",
            TranslationReviewState.Review => "Needs review.",
            _ => "Missing target value."
        };
    }

    private static string EnglishSource(string key)
    {
        return key switch
        {
            "Shell.FileMenu" => "File",
            "Shell.Import" => "Import catalog",
            "Shell.Export" => "Export catalog",
            "Orders.EmptyState" => "No orders require translation.",
            "Orders.Total" => "Total: {0}",
            "Orders.DueDate" => "Due by {0:D}",
            "Region.MeasurementSystem" => "Measurement system: {0}",
            "Diagnostics.ProviderFailed" => "Provider {0} failed: {1}",
            _ => key
        };
    }

    private static string Translate(string cultureName, string sourceText)
    {
        return cultureName switch
        {
            "pl-PL" => sourceText switch
            {
                "File" => "Plik",
                "Import catalog" => "Importuj katalog",
                "Export catalog" => "Eksportuj katalog",
                "Total: {0}" => "Suma: {0}",
                "Due by {0:D}" => "Termin: {0:D}",
                "Measurement system: {0}" => "System miar: {0}",
                "Provider {0} failed: {1}" => "Dostawca {0} zwrocil blad: {1}",
                _ => sourceText
            },
            "de-DE" => sourceText switch
            {
                "File" => "Datei",
                "Import catalog" => "Katalog importieren",
                "Export catalog" => "Katalog exportieren",
                "Total: {0}" => "Summe: {0}",
                "Due by {0:D}" => "Faellig bis {0:D}",
                "Measurement system: {0}" => "Masssystem: {0}",
                "Provider {0} failed: {1}" => "Anbieter {0} ist fehlgeschlagen: {1}",
                _ => sourceText
            },
            "ja-JP" => sourceText switch
            {
                "File" => "File",
                "Import catalog" => "Import catalog",
                "Export catalog" => "Export catalog",
                "Total: {0}" => "Total: {0}",
                "Due by {0:D}" => "Due by {0:D}",
                "Measurement system: {0}" => "Measurement system: {0}",
                "Provider {0} failed: {1}" => "Provider {0} failed: {1}",
                _ => sourceText
            },
            _ => sourceText
        };
    }
}
