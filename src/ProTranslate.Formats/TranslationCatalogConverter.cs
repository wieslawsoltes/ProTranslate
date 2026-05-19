using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;

namespace ProTranslate.Formats;

public static class TranslationCatalogConverter
{
    private static readonly XNamespace Xliff12Namespace = "urn:oasis:names:tc:xliff:document:1.2";
    private static readonly XNamespace Xliff20Namespace = "urn:oasis:names:tc:xliff:document:2.0";

    public static TranslationFileFormat DetectFormat(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fileName = Path.GetFileName(path);
        string extension = Path.GetExtension(path);

        if (fileName.EndsWith(".xcstrings", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationFileFormat.AppleXcstrings;
        }

        if (fileName.EndsWith(".stringsdict", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationFileFormat.AppleStringsdict;
        }

        if (fileName.EndsWith(".strings", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationFileFormat.AppleStrings;
        }

        if (fileName.EndsWith(".arb", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationFileFormat.FlutterArb;
        }

        if (fileName.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationFileFormat.Resx;
        }

        if (fileName.EndsWith(".po", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".pot", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationFileFormat.GettextPo;
        }

        if (fileName.EndsWith(".xlf", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".xliff", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationFileFormat.Xliff12;
        }

        if (fileName.Equals("strings.xml", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".strings.xml", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationFileFormat.AndroidResources;
        }

        if (extension.Equals(".tsv", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationFileFormat.Tsv;
        }

        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationFileFormat.Csv;
        }

        if (fileName.EndsWith(".i18next.json", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".i18n.json", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationFileFormat.I18NextJson;
        }

        return TranslationFileFormat.ProTranslateJson;
    }

    public static TranslationFormatResult Import(
        string content,
        TranslationFileFormat format,
        TranslationFormatOptions? options = null,
        string? path = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        options ??= new TranslationFormatOptions();
        var catalog = new TranslationCatalog
        {
            Name = options.Name ?? (path is null ? null : Path.GetFileNameWithoutExtension(path)),
            SourceCulture = options.SourceCulture
        };
        var diagnostics = new List<TranslationFormatDiagnostic>();

        try
        {
            switch (format)
            {
                case TranslationFileFormat.ProTranslateJson:
                case TranslationFileFormat.I18NextJson:
                    ImportJson(content, catalog, options, diagnostics, skipMetadataKeys: false);
                    break;
                case TranslationFileFormat.FlutterArb:
                    ImportJson(content, catalog, options, diagnostics, skipMetadataKeys: true);
                    break;
                case TranslationFileFormat.AppleXcstrings:
                    ImportXcstrings(content, catalog, diagnostics);
                    break;
                case TranslationFileFormat.Resx:
                    ImportResx(content, catalog, options, path, diagnostics);
                    break;
                case TranslationFileFormat.AndroidResources:
                    ImportAndroidResources(content, catalog, options, path, diagnostics);
                    break;
                case TranslationFileFormat.AppleStrings:
                    ImportAppleStrings(content, catalog, options, path, diagnostics);
                    break;
                case TranslationFileFormat.AppleStringsdict:
                    ImportAppleStringsdict(content, catalog, options, path, diagnostics);
                    break;
                case TranslationFileFormat.GettextPo:
                    ImportPo(content, catalog, options, path, diagnostics);
                    break;
                case TranslationFileFormat.Xliff12:
                case TranslationFileFormat.Xliff20:
                    ImportXliff(content, catalog, diagnostics);
                    break;
                case TranslationFileFormat.Csv:
                    ImportDelimited(content, catalog, options, diagnostics, ',');
                    break;
                case TranslationFileFormat.Tsv:
                    ImportDelimited(content, catalog, options, diagnostics, '\t');
                    break;
                default:
                    diagnostics.Add(new TranslationFormatDiagnostic(TranslationFormatDiagnosticSeverity.Error, "PTF000", $"Unsupported format '{format}'."));
                    break;
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or FormatException or System.Xml.XmlException)
        {
            diagnostics.Add(new TranslationFormatDiagnostic(TranslationFormatDiagnosticSeverity.Error, "PTF001", ex.Message));
        }

        return new TranslationFormatResult(catalog, diagnostics);
    }

    public static string Export(
        TranslationCatalog catalog,
        TranslationFileFormat format,
        TranslationFormatOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        options ??= new TranslationFormatOptions();

        return format switch
        {
            TranslationFileFormat.ProTranslateJson or TranslationFileFormat.I18NextJson => ExportJson(catalog, options, includeArbMetadata: false),
            TranslationFileFormat.FlutterArb => ExportJson(catalog, options, includeArbMetadata: true),
            TranslationFileFormat.AppleXcstrings => ExportXcstrings(catalog, options),
            TranslationFileFormat.Resx => ExportResx(catalog, options),
            TranslationFileFormat.AndroidResources => ExportAndroidResources(catalog, options),
            TranslationFileFormat.AppleStrings => ExportAppleStrings(catalog, options),
            TranslationFileFormat.AppleStringsdict => ExportAppleStringsdict(catalog, options),
            TranslationFileFormat.GettextPo => ExportPo(catalog, options),
            TranslationFileFormat.Xliff12 => ExportXliff12(catalog, options),
            TranslationFileFormat.Xliff20 => ExportXliff20(catalog, options),
            TranslationFileFormat.Csv => ExportDelimited(catalog, options, ','),
            TranslationFileFormat.Tsv => ExportDelimited(catalog, options, '\t'),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported export format.")
        };
    }

    private static void ImportJson(
        string content,
        TranslationCatalog catalog,
        TranslationFormatOptions options,
        List<TranslationFormatDiagnostic> diagnostics,
        bool skipMetadataKeys)
    {
        using JsonDocument document = JsonDocument.Parse(content, new JsonDocumentOptions { AllowTrailingCommas = true });
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("JSON translation catalog root must be an object.");
        }

        string? culture = options.Culture;
        if (skipMetadataKeys && document.RootElement.TryGetProperty("@@locale", out JsonElement locale) && locale.ValueKind == JsonValueKind.String)
        {
            culture = locale.GetString();
        }

        FlattenJson(document.RootElement, string.Empty, culture, catalog, diagnostics, skipMetadataKeys);
        if (skipMetadataKeys)
        {
            ApplyArbMetadata(document.RootElement, catalog);
        }
    }

    private static void FlattenJson(
        JsonElement element,
        string prefix,
        string? culture,
        TranslationCatalog catalog,
        List<TranslationFormatDiagnostic> diagnostics,
        bool skipMetadataKeys)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (skipMetadataKeys && property.Name.StartsWith('@'))
            {
                continue;
            }

            string key = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                catalog.Add(key, culture, property.Value.GetString() ?? string.Empty);
            }
            else if (property.Value.ValueKind == JsonValueKind.Object)
            {
                FlattenJson(property.Value, key, culture, catalog, diagnostics, skipMetadataKeys);
            }
            else
            {
                diagnostics.Add(new TranslationFormatDiagnostic(
                    TranslationFormatDiagnosticSeverity.Info,
                    "PTF101",
                    $"Ignored non-string JSON value at '{key}'.",
                    key,
                    culture));
            }
        }
    }

    private static void ApplyArbMetadata(JsonElement root, TranslationCatalog catalog)
    {
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (!property.Name.StartsWith('@') || property.Name.Equals("@@locale", StringComparison.Ordinal) || property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string key = property.Name[1..];
            if (key.Length == 0 || !property.Value.TryGetProperty("description", out JsonElement description) || description.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            foreach (TranslationCatalogEntry entry in catalog.Entries.Where(entry => string.Equals(entry.Key, key, StringComparison.Ordinal)))
            {
                entry.Comment ??= description.GetString();
            }
        }
    }

    private static void ImportXcstrings(string content, TranslationCatalog catalog, List<TranslationFormatDiagnostic> diagnostics)
    {
        using JsonDocument document = JsonDocument.Parse(content, new JsonDocumentOptions { AllowTrailingCommas = true });
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("strings", out JsonElement strings) || strings.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Apple .xcstrings root must contain a 'strings' object.");
        }

        if (root.TryGetProperty("sourceLanguage", out JsonElement sourceLanguage) && sourceLanguage.ValueKind == JsonValueKind.String)
        {
            catalog.SourceCulture = sourceLanguage.GetString();
        }

        foreach (JsonProperty stringProperty in strings.EnumerateObject())
        {
            if (stringProperty.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string key = stringProperty.Name;
            string? entryComment = null;
            if (stringProperty.Value.TryGetProperty("comment", out JsonElement comment) && comment.ValueKind == JsonValueKind.String)
            {
                entryComment = comment.GetString();
            }

            if (!stringProperty.Value.TryGetProperty("localizations", out JsonElement localizations) || localizations.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (JsonProperty localization in localizations.EnumerateObject())
            {
                string culture = localization.Name;
                if (TryGetXcstringsValue(localization.Value, out string? value, out string? state))
                {
                    TranslationCatalogEntry entry = catalog.Add(key, culture, value);
                    entry.State = state;
                    entry.Comment = entryComment;
                }
                else if (TryAddXcstringsPluralVariations(catalog, key, culture, localization.Value, entryComment))
                {
                }
                else
                {
                    diagnostics.Add(new TranslationFormatDiagnostic(
                        TranslationFormatDiagnosticSeverity.Warning,
                        "PTF120",
                        "Unsupported .xcstrings variation was skipped.",
                        key,
                        culture));
                }
            }
        }
    }

    private static bool TryAddXcstringsPluralVariations(
        TranslationCatalog catalog,
        string key,
        string culture,
        JsonElement localization,
        string? comment)
    {
        if (localization.ValueKind != JsonValueKind.Object
            || !localization.TryGetProperty("variations", out JsonElement variations)
            || variations.ValueKind != JsonValueKind.Object
            || !variations.TryGetProperty("plural", out JsonElement plural)
            || plural.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        bool added = false;
        foreach (JsonProperty category in plural.EnumerateObject())
        {
            if (!TryGetXcstringsValue(category.Value, out string value, out string? state))
            {
                continue;
            }

            TranslationCatalogEntry entry = catalog.Add(key + "." + category.Name, culture, value);
            entry.State = state;
            entry.Comment = comment;
            entry.PluralCategory = category.Name;
            added = true;
        }

        return added;
    }

    private static bool TryGetXcstringsValue(JsonElement localization, out string value, out string? state)
    {
        value = string.Empty;
        state = null;
        if (localization.ValueKind != JsonValueKind.Object || !localization.TryGetProperty("stringUnit", out JsonElement unit))
        {
            return false;
        }

        if (unit.TryGetProperty("state", out JsonElement stateElement) && stateElement.ValueKind == JsonValueKind.String)
        {
            state = stateElement.GetString();
        }

        if (unit.TryGetProperty("value", out JsonElement valueElement) && valueElement.ValueKind == JsonValueKind.String)
        {
            value = valueElement.GetString() ?? string.Empty;
            return true;
        }

        return false;
    }

    private static void ImportResx(
        string content,
        TranslationCatalog catalog,
        TranslationFormatOptions options,
        string? path,
        List<TranslationFormatDiagnostic> diagnostics)
    {
        XDocument document = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        string? culture = options.Culture ?? InferCultureFromPath(path, ".resx");

        foreach (XElement data in document.Descendants("data"))
        {
            string? key = data.Attribute("name")?.Value;
            XElement? value = data.Element("value");
            if (string.IsNullOrWhiteSpace(key) || value is null)
            {
                diagnostics.Add(new TranslationFormatDiagnostic(TranslationFormatDiagnosticSeverity.Warning, "PTF130", "Ignored .resx data node without name or value."));
                continue;
            }

            TranslationCatalogEntry entry = catalog.Add(key, culture, value.Value);
            entry.Comment = data.Element("comment")?.Value;
        }
    }

    private static void ImportAndroidResources(
        string content,
        TranslationCatalog catalog,
        TranslationFormatOptions options,
        string? path,
        List<TranslationFormatDiagnostic> diagnostics)
    {
        XDocument document = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        XElement root = document.Root ?? throw new FormatException("Android strings.xml must have a resources root.");
        string? culture = options.Culture ?? InferAndroidCulture(path);

        foreach (XElement child in root.Elements())
        {
            string? name = child.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            switch (child.Name.LocalName)
            {
                case "string":
                    catalog.Add(name, culture, child.Value);
                    break;
                case "plurals":
                    foreach (XElement item in child.Elements("item"))
                    {
                        string? quantity = item.Attribute("quantity")?.Value;
                        if (string.IsNullOrWhiteSpace(quantity))
                        {
                            continue;
                        }

                        TranslationCatalogEntry entry = catalog.Add(name + "." + quantity, culture, item.Value);
                        entry.PluralCategory = quantity;
                    }

                    break;
                case "string-array":
                    int index = 0;
                    foreach (XElement item in child.Elements("item"))
                    {
                        catalog.Add(name + "[" + index.ToString(CultureInfo.InvariantCulture) + "]", culture, item.Value);
                        index++;
                    }

                    break;
                default:
                    diagnostics.Add(new TranslationFormatDiagnostic(TranslationFormatDiagnosticSeverity.Info, "PTF140", $"Ignored Android resource '{child.Name.LocalName}'.", name, culture));
                    break;
            }
        }
    }

    private static void ImportAppleStrings(
        string content,
        TranslationCatalog catalog,
        TranslationFormatOptions options,
        string? path,
        List<TranslationFormatDiagnostic> diagnostics)
    {
        string? culture = options.Culture ?? InferAppleCulture(path);
        string? pendingComment = null;
        int index = 0;

        while (index < content.Length)
        {
            SkipWhitespaceAndAppleComments(content, ref index, ref pendingComment);
            if (index >= content.Length)
            {
                break;
            }

            if (!TryReadAppleQuotedString(content, ref index, out string key))
            {
                diagnostics.Add(new TranslationFormatDiagnostic(TranslationFormatDiagnosticSeverity.Warning, "PTF150", "Ignored malformed .strings entry."));
                break;
            }

            SkipWhitespaceAndAppleComments(content, ref index, ref pendingComment);
            if (index >= content.Length || content[index] != '=')
            {
                diagnostics.Add(new TranslationFormatDiagnostic(TranslationFormatDiagnosticSeverity.Warning, "PTF151", "Ignored .strings entry without '='.", key, culture));
                break;
            }

            index++;
            SkipWhitespaceAndAppleComments(content, ref index, ref pendingComment);
            if (!TryReadAppleQuotedString(content, ref index, out string value))
            {
                diagnostics.Add(new TranslationFormatDiagnostic(TranslationFormatDiagnosticSeverity.Warning, "PTF152", "Ignored .strings entry without value.", key, culture));
                break;
            }

            while (index < content.Length && content[index] != ';')
            {
                index++;
            }

            if (index < content.Length)
            {
                index++;
            }

            TranslationCatalogEntry entry = catalog.Add(key, culture, value);
            entry.Comment = pendingComment;
            pendingComment = null;
        }
    }

    private static void ImportAppleStringsdict(
        string content,
        TranslationCatalog catalog,
        TranslationFormatOptions options,
        string? path,
        List<TranslationFormatDiagnostic> diagnostics)
    {
        XDocument document = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        XElement? dict = document.Root?.Element("dict");
        if (dict is null)
        {
            throw new FormatException(".stringsdict must contain a plist/dict root.");
        }

        string? culture = options.Culture ?? InferAppleCulture(path);
        Dictionary<string, XElement> top = ReadPlistDict(dict);
        foreach (KeyValuePair<string, XElement> entry in top)
        {
            Dictionary<string, XElement> valueDict = ReadPlistDict(entry.Value);
            foreach (KeyValuePair<string, XElement> child in valueDict)
            {
                if (child.Value.Name.LocalName != "dict")
                {
                    continue;
                }

                Dictionary<string, XElement> pluralDict = ReadPlistDict(child.Value);
                if (!pluralDict.TryGetValue("NSStringFormatSpecTypeKey", out XElement? spec)
                    || !string.Equals(spec.Value, "NSStringPluralRuleType", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (string category in new[] { "zero", "one", "two", "few", "many", "other" })
                {
                    if (pluralDict.TryGetValue(category, out XElement? localized))
                    {
                        TranslationCatalogEntry catalogEntry = catalog.Add(entry.Key + "." + category, culture, localized.Value);
                        catalogEntry.PluralCategory = category;
                    }
                }
            }
        }

        if (catalog.Entries.Count == 0)
        {
            diagnostics.Add(new TranslationFormatDiagnostic(TranslationFormatDiagnosticSeverity.Warning, "PTF160", "No supported plural entries were found in .stringsdict."));
        }
    }

    private static void ImportPo(
        string content,
        TranslationCatalog catalog,
        TranslationFormatOptions options,
        string? path,
        List<TranslationFormatDiagnostic> diagnostics)
    {
        string? culture = options.Culture ?? InferCultureFromPath(path, ".po") ?? InferCultureFromPath(path, ".pot");
        foreach (PoEntry entry in ParsePoEntries(content))
        {
            if (entry.MsgId.Length == 0)
            {
                string? headerLanguage = ReadPoHeaderLanguage(entry.MsgStr);
                if (!string.IsNullOrWhiteSpace(headerLanguage))
                {
                    culture = headerLanguage;
                }

                continue;
            }

            string key = entry.Context is null ? entry.MsgId : entry.Context + "\u0004" + entry.MsgId;
            if (entry.PluralTranslations.Count == 0)
            {
                TranslationCatalogEntry catalogEntry = catalog.Add(key, culture, entry.MsgStr.Length == 0 ? entry.MsgId : entry.MsgStr);
                catalogEntry.Context = entry.Context;
                catalogEntry.Comment = entry.Comment;
                catalogEntry.Source = entry.MsgId;
            }
            else
            {
                foreach (KeyValuePair<int, string> plural in entry.PluralTranslations.OrderBy(static value => value.Key))
                {
                    string category = plural.Key == 0 ? "one" : "other";
                    TranslationCatalogEntry catalogEntry = catalog.Add(key + "." + category, culture, plural.Value);
                    catalogEntry.Context = entry.Context;
                    catalogEntry.Comment = entry.Comment;
                    catalogEntry.Source = plural.Key == 0 ? entry.MsgId : entry.MsgIdPlural;
                    catalogEntry.PluralCategory = category;
                }
            }
        }

        if (catalog.Entries.Count == 0)
        {
            diagnostics.Add(new TranslationFormatDiagnostic(TranslationFormatDiagnosticSeverity.Warning, "PTF170", "No gettext PO entries were imported."));
        }
    }

    private static void ImportXliff(string content, TranslationCatalog catalog, List<TranslationFormatDiagnostic> diagnostics)
    {
        XDocument document = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        XElement root = document.Root ?? throw new FormatException("XLIFF document must have a root element.");
        string version = root.Attribute("version")?.Value ?? string.Empty;

        if (version.StartsWith("1.", StringComparison.Ordinal))
        {
            ImportXliff12(root, catalog);
        }
        else
        {
            ImportXliff20(root, catalog);
        }

        if (catalog.Entries.Count == 0)
        {
            diagnostics.Add(new TranslationFormatDiagnostic(TranslationFormatDiagnosticSeverity.Warning, "PTF180", "No XLIFF units were imported."));
        }
    }

    private static void ImportXliff12(XElement root, TranslationCatalog catalog)
    {
        foreach (XElement file in root.Descendants(Xliff12Namespace + "file").Concat(root.Descendants("file")))
        {
            string? sourceCulture = file.Attribute("source-language")?.Value;
            string? targetCulture = file.Attribute("target-language")?.Value;
            catalog.SourceCulture ??= sourceCulture;

            IEnumerable<XElement> units = file.Descendants(Xliff12Namespace + "trans-unit").Concat(file.Descendants("trans-unit"));
            foreach (XElement unit in units)
            {
                string key = unit.Attribute("resname")?.Value ?? unit.Attribute("id")?.Value ?? Guid.NewGuid().ToString("N");
                string? source = unit.Element(Xliff12Namespace + "source")?.Value ?? unit.Element("source")?.Value;
                string? target = unit.Element(Xliff12Namespace + "target")?.Value ?? unit.Element("target")?.Value;
                string value = target ?? source ?? string.Empty;
                TranslationCatalogEntry entry = catalog.Add(key, targetCulture, value);
                entry.Source = source;
                entry.State = unit.Element(Xliff12Namespace + "target")?.Attribute("state")?.Value ?? unit.Element("target")?.Attribute("state")?.Value;
                entry.Comment = unit.Elements(Xliff12Namespace + "note").Concat(unit.Elements("note")).FirstOrDefault()?.Value;
            }
        }
    }

    private static void ImportXliff20(XElement root, TranslationCatalog catalog)
    {
        string? sourceCulture = root.Attribute("srcLang")?.Value;
        string? targetCulture = root.Attribute("trgLang")?.Value;
        catalog.SourceCulture ??= sourceCulture;

        foreach (XElement file in root.Descendants(Xliff20Namespace + "file").Concat(root.Descendants("file")))
        {
            foreach (XElement unit in file.Descendants(Xliff20Namespace + "unit").Concat(file.Descendants("unit")))
            {
                string key = unit.Attribute("name")?.Value ?? unit.Attribute("id")?.Value ?? Guid.NewGuid().ToString("N");
                foreach (XElement segment in unit.Descendants(Xliff20Namespace + "segment").Concat(unit.Descendants("segment")))
                {
                    string? source = segment.Element(Xliff20Namespace + "source")?.Value ?? segment.Element("source")?.Value;
                    string? target = segment.Element(Xliff20Namespace + "target")?.Value ?? segment.Element("target")?.Value;
                    string value = target ?? source ?? string.Empty;
                    TranslationCatalogEntry entry = catalog.Add(key, targetCulture, value);
                    entry.Source = source;
                    entry.State = segment.Attribute("state")?.Value;
                    entry.Comment = unit.Descendants(Xliff20Namespace + "note").Concat(unit.Descendants("note")).FirstOrDefault()?.Value;
                }
            }
        }
    }

    private static void ImportDelimited(
        string content,
        TranslationCatalog catalog,
        TranslationFormatOptions options,
        List<TranslationFormatDiagnostic> diagnostics,
        char delimiter)
    {
        List<string[]> rows = ParseDelimitedRows(content, delimiter);
        if (rows.Count == 0)
        {
            return;
        }

        Dictionary<string, int> columns = rows[0]
            .Select(static (name, index) => new { Name = name.Trim().ToLowerInvariant(), Index = index })
            .ToDictionary(static item => item.Name, static item => item.Index, StringComparer.Ordinal);

        if (!columns.ContainsKey("key") || !columns.ContainsKey("value"))
        {
            diagnostics.Add(new TranslationFormatDiagnostic(TranslationFormatDiagnosticSeverity.Error, "PTF190", "Delimited catalogs require 'key' and 'value' columns."));
            return;
        }

        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];
            string key = GetColumn(row, columns, "key");
            string value = GetColumn(row, columns, "value");
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            TranslationCatalogEntry entry = catalog.Add(key, GetColumn(row, columns, "culture", options.Culture), value);
            entry.Source = GetColumn(row, columns, "source", null);
            entry.Comment = GetColumn(row, columns, "comment", null);
            entry.Context = GetColumn(row, columns, "context", null);
            entry.State = GetColumn(row, columns, "state", null);
            entry.PluralCategory = GetColumn(row, columns, "plural", null);
            entry.Format = GetColumn(row, columns, "format", null);
        }
    }

    private static string ExportJson(TranslationCatalog catalog, TranslationFormatOptions options, bool includeArbMetadata)
    {
        string? culture = options.Culture ?? catalog.Cultures.FirstOrDefault();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            if (includeArbMetadata && !string.IsNullOrWhiteSpace(culture))
            {
                writer.WriteString("@@locale", culture);
            }

            foreach (TranslationCatalogEntry entry in SelectEntries(catalog, culture))
            {
                writer.WriteString(entry.Key, entry.Value);
                if (includeArbMetadata && !string.IsNullOrWhiteSpace(entry.Comment))
                {
                    writer.WritePropertyName("@" + entry.Key);
                    writer.WriteStartObject();
                    writer.WriteString("description", entry.Comment);
                    writer.WriteEndObject();
                }
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string ExportXcstrings(TranslationCatalog catalog, TranslationFormatOptions options)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteString("sourceLanguage", options.SourceCulture ?? catalog.SourceCulture ?? "en");
            writer.WriteString("version", "1.0");
            writer.WritePropertyName("strings");
            writer.WriteStartObject();

            foreach (IGrouping<string, TranslationCatalogEntry> group in catalog.Entries
                .GroupBy(static entry => string.IsNullOrWhiteSpace(entry.PluralCategory) ? entry.Key : TrimPluralSuffix(entry), StringComparer.Ordinal)
                .OrderBy(static group => group.Key, StringComparer.Ordinal))
            {
                writer.WritePropertyName(group.Key);
                writer.WriteStartObject();
                string? comment = group.Select(static entry => entry.Comment).FirstOrDefault(static comment => !string.IsNullOrWhiteSpace(comment));
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    writer.WriteString("comment", comment);
                }

                writer.WritePropertyName("localizations");
                writer.WriteStartObject();
                foreach (IGrouping<string, TranslationCatalogEntry> cultureGroup in group
                    .Where(static entry => !string.IsNullOrWhiteSpace(entry.Culture))
                    .GroupBy(static entry => entry.Culture!, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static cultureGroup => cultureGroup.Key, StringComparer.OrdinalIgnoreCase))
                {
                    writer.WritePropertyName(cultureGroup.Key);
                    writer.WriteStartObject();
                    List<TranslationCatalogEntry> pluralEntries = cultureGroup
                        .Where(static entry => !string.IsNullOrWhiteSpace(entry.PluralCategory))
                        .OrderBy(static entry => entry.PluralCategory, StringComparer.Ordinal)
                        .ToList();
                    if (pluralEntries.Count > 0)
                    {
                        writer.WritePropertyName("variations");
                        writer.WriteStartObject();
                        writer.WritePropertyName("plural");
                        writer.WriteStartObject();
                        foreach (TranslationCatalogEntry entry in pluralEntries)
                        {
                            writer.WritePropertyName(entry.PluralCategory!);
                            writer.WriteStartObject();
                            WriteXcstringsStringUnit(writer, entry);
                            writer.WriteEndObject();
                        }

                        writer.WriteEndObject();
                        writer.WriteEndObject();
                    }
                    else
                    {
                        WriteXcstringsStringUnit(writer, cultureGroup.Last());
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteXcstringsStringUnit(Utf8JsonWriter writer, TranslationCatalogEntry entry)
    {
        writer.WritePropertyName("stringUnit");
        writer.WriteStartObject();
        writer.WriteString("state", entry.State ?? "translated");
        writer.WriteString("value", entry.Value);
        writer.WriteEndObject();
    }

    private static string ExportResx(TranslationCatalog catalog, TranslationFormatOptions options)
    {
        var root = new XElement("root",
            new XElement("resheader", new XAttribute("name", "resmimetype"), new XElement("value", "text/microsoft-resx")),
            new XElement("resheader", new XAttribute("name", "version"), new XElement("value", "2.0")),
            new XElement("resheader", new XAttribute("name", "reader"), new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms")),
            new XElement("resheader", new XAttribute("name", "writer"), new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms")));

        foreach (TranslationCatalogEntry entry in SelectEntries(catalog, options.Culture))
        {
            var data = new XElement("data", new XAttribute("name", entry.Key), new XAttribute(XNamespace.Xml + "space", "preserve"), new XElement("value", entry.Value));
            if (options.IncludeComments && !string.IsNullOrWhiteSpace(entry.Comment))
            {
                data.Add(new XElement("comment", entry.Comment));
            }

            root.Add(data);
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString(SaveOptions.None);
    }

    private static string ExportAndroidResources(TranslationCatalog catalog, TranslationFormatOptions options)
    {
        var root = new XElement("resources");
        foreach (TranslationCatalogEntry entry in SelectEntries(catalog, options.Culture).Where(static entry => string.IsNullOrWhiteSpace(entry.PluralCategory)))
        {
            root.Add(new XElement("string", new XAttribute("name", entry.Key), entry.Value));
        }

        foreach (IGrouping<string, TranslationCatalogEntry> group in SelectEntries(catalog, options.Culture)
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.PluralCategory))
            .GroupBy(static entry => TrimPluralSuffix(entry), StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            var plurals = new XElement("plurals", new XAttribute("name", group.Key));
            foreach (TranslationCatalogEntry entry in group.OrderBy(static entry => entry.PluralCategory, StringComparer.Ordinal))
            {
                plurals.Add(new XElement("item", new XAttribute("quantity", entry.PluralCategory!), entry.Value));
            }

            root.Add(plurals);
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString(SaveOptions.None);
    }

    private static string ExportAppleStrings(TranslationCatalog catalog, TranslationFormatOptions options)
    {
        var builder = new StringBuilder();
        foreach (TranslationCatalogEntry entry in SelectEntries(catalog, options.Culture))
        {
            if (options.IncludeComments && !string.IsNullOrWhiteSpace(entry.Comment))
            {
                builder.Append("/* ");
                builder.Append(entry.Comment.Replace("*/", "* /", StringComparison.Ordinal));
                builder.AppendLine(" */");
            }

            builder.Append('"');
            builder.Append(EscapeAppleString(entry.Key));
            builder.Append("\" = \"");
            builder.Append(EscapeAppleString(entry.Value));
            builder.AppendLine("\";");
        }

        return builder.ToString();
    }

    private static string ExportAppleStringsdict(TranslationCatalog catalog, TranslationFormatOptions options)
    {
        var rootDict = new XElement("dict");
        foreach (IGrouping<string, TranslationCatalogEntry> group in SelectEntries(catalog, options.Culture)
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.PluralCategory))
            .GroupBy(static entry => TrimPluralSuffix(entry), StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            rootDict.Add(new XElement("key", group.Key));
            var entryDict = new XElement("dict",
                new XElement("key", "NSStringLocalizedFormatKey"),
                new XElement("string", "%#@value@"),
                new XElement("key", "value"));
            var pluralDict = new XElement("dict",
                new XElement("key", "NSStringFormatSpecTypeKey"),
                new XElement("string", "NSStringPluralRuleType"),
                new XElement("key", "NSStringFormatValueTypeKey"),
                new XElement("string", "d"));

            foreach (TranslationCatalogEntry entry in group.OrderBy(static entry => entry.PluralCategory, StringComparer.Ordinal))
            {
                pluralDict.Add(new XElement("key", entry.PluralCategory!), new XElement("string", entry.Value));
            }

            entryDict.Add(pluralDict);
            rootDict.Add(entryDict);
        }

        var plist = new XElement("plist", new XAttribute("version", "1.0"), rootDict);
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null),
            plist);
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static string ExportPo(TranslationCatalog catalog, TranslationFormatOptions options)
    {
        string? culture = options.Culture ?? catalog.Cultures.FirstOrDefault();
        var builder = new StringBuilder();
        builder.AppendLine("msgid \"\"");
        builder.AppendLine("msgstr \"\"");
        if (!string.IsNullOrWhiteSpace(culture))
        {
            builder.Append("\"Language: ");
            builder.Append(EscapePo(culture));
            builder.AppendLine("\\n\"");
        }

        builder.AppendLine();

        List<TranslationCatalogEntry> entries = SelectEntries(catalog, culture).ToList();
        foreach (TranslationCatalogEntry entry in entries.Where(static entry => string.IsNullOrWhiteSpace(entry.PluralCategory)))
        {
            if (options.IncludeComments && !string.IsNullOrWhiteSpace(entry.Comment))
            {
                builder.Append("#. ");
                builder.AppendLine(entry.Comment);
            }

            if (!string.IsNullOrWhiteSpace(entry.Context))
            {
                builder.Append("msgctxt \"");
                builder.Append(EscapePo(entry.Context));
                builder.AppendLine("\"");
            }

            builder.Append("msgid \"");
            builder.Append(EscapePo(entry.Source ?? entry.Key));
            builder.AppendLine("\"");
            builder.Append("msgstr \"");
            builder.Append(EscapePo(entry.Value));
            builder.AppendLine("\"");
            builder.AppendLine();
        }

        foreach (IGrouping<string, TranslationCatalogEntry> group in entries
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.PluralCategory))
            .GroupBy(static entry => TrimPluralSuffix(entry), StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            TranslationCatalogEntry? singular = group.FirstOrDefault(static entry => string.Equals(entry.PluralCategory, "one", StringComparison.Ordinal));
            TranslationCatalogEntry? plural = group.FirstOrDefault(static entry => string.Equals(entry.PluralCategory, "other", StringComparison.Ordinal))
                ?? group.FirstOrDefault(static entry => !string.Equals(entry.PluralCategory, "one", StringComparison.Ordinal));
            if (singular is null || plural is null)
            {
                foreach (TranslationCatalogEntry entry in group)
                {
                    WritePoSingularEntry(builder, entry, options);
                }

                continue;
            }

            if (options.IncludeComments && !string.IsNullOrWhiteSpace(singular.Comment))
            {
                builder.Append("#. ");
                builder.AppendLine(singular.Comment);
            }

            if (!string.IsNullOrWhiteSpace(singular.Context))
            {
                builder.Append("msgctxt \"");
                builder.Append(EscapePo(singular.Context));
                builder.AppendLine("\"");
            }

            builder.Append("msgid \"");
            builder.Append(EscapePo(singular.Source ?? singular.Key));
            builder.AppendLine("\"");
            builder.Append("msgid_plural \"");
            builder.Append(EscapePo(plural.Source ?? plural.Key));
            builder.AppendLine("\"");
            builder.Append("msgstr[0] \"");
            builder.Append(EscapePo(singular.Value));
            builder.AppendLine("\"");
            builder.Append("msgstr[1] \"");
            builder.Append(EscapePo(plural.Value));
            builder.AppendLine("\"");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void WritePoSingularEntry(StringBuilder builder, TranslationCatalogEntry entry, TranslationFormatOptions options)
    {
        if (options.IncludeComments && !string.IsNullOrWhiteSpace(entry.Comment))
        {
            builder.Append("#. ");
            builder.AppendLine(entry.Comment);
        }

        if (!string.IsNullOrWhiteSpace(entry.Context))
        {
            builder.Append("msgctxt \"");
            builder.Append(EscapePo(entry.Context));
            builder.AppendLine("\"");
        }

        builder.Append("msgid \"");
        builder.Append(EscapePo(entry.Source ?? entry.Key));
        builder.AppendLine("\"");
        builder.Append("msgstr \"");
        builder.Append(EscapePo(entry.Value));
        builder.AppendLine("\"");
        builder.AppendLine();
    }

    private static string ExportXliff12(TranslationCatalog catalog, TranslationFormatOptions options)
    {
        string targetCulture = options.Culture ?? catalog.Cultures.FirstOrDefault() ?? "und";
        var body = new XElement(Xliff12Namespace + "body");
        foreach (TranslationCatalogEntry entry in SelectEntries(catalog, targetCulture))
        {
            var unit = new XElement(Xliff12Namespace + "trans-unit",
                new XAttribute("id", entry.Key),
                new XAttribute("resname", entry.Key),
                new XElement(Xliff12Namespace + "source", entry.Source ?? entry.Key),
                new XElement(Xliff12Namespace + "target", entry.Value));
            if (options.IncludeComments && !string.IsNullOrWhiteSpace(entry.Comment))
            {
                unit.Add(new XElement(Xliff12Namespace + "note", entry.Comment));
            }

            body.Add(unit);
        }

        var doc = new XDocument(
            new XElement(Xliff12Namespace + "xliff",
                new XAttribute("version", "1.2"),
                new XElement(Xliff12Namespace + "file",
                    new XAttribute("source-language", options.SourceCulture ?? catalog.SourceCulture ?? "en"),
                    new XAttribute("target-language", targetCulture),
                    new XAttribute("datatype", "plaintext"),
                    new XAttribute("original", catalog.Name ?? "ProTranslate"),
                    body)));
        return doc.ToString(SaveOptions.None);
    }

    private static string ExportXliff20(TranslationCatalog catalog, TranslationFormatOptions options)
    {
        string targetCulture = options.Culture ?? catalog.Cultures.FirstOrDefault() ?? "und";
        var file = new XElement(Xliff20Namespace + "file", new XAttribute("id", catalog.Name ?? "ProTranslate"));
        foreach (TranslationCatalogEntry entry in SelectEntries(catalog, targetCulture))
        {
            var unit = new XElement(Xliff20Namespace + "unit",
                new XAttribute("id", entry.Key),
                new XAttribute("name", entry.Key),
                new XElement(Xliff20Namespace + "segment",
                    new XElement(Xliff20Namespace + "source", entry.Source ?? entry.Key),
                    new XElement(Xliff20Namespace + "target", entry.Value)));
            if (options.IncludeComments && !string.IsNullOrWhiteSpace(entry.Comment))
            {
                unit.Add(new XElement(Xliff20Namespace + "notes", new XElement(Xliff20Namespace + "note", entry.Comment)));
            }

            file.Add(unit);
        }

        var doc = new XDocument(
            new XElement(Xliff20Namespace + "xliff",
                new XAttribute("version", "2.1"),
                new XAttribute("srcLang", options.SourceCulture ?? catalog.SourceCulture ?? "en"),
                new XAttribute("trgLang", targetCulture),
                file));
        return doc.ToString(SaveOptions.None);
    }

    private static string ExportDelimited(TranslationCatalog catalog, TranslationFormatOptions options, char delimiter)
    {
        var builder = new StringBuilder();
        string[] headers = ["key", "culture", "value", "source", "comment", "context", "state", "plural", "format"];
        AppendDelimitedRow(builder, headers, delimiter);

        foreach (TranslationCatalogEntry entry in catalog.Entries.OrderBy(static entry => entry.Key, StringComparer.Ordinal).ThenBy(static entry => entry.Culture, StringComparer.OrdinalIgnoreCase))
        {
            AppendDelimitedRow(builder, [
                entry.Key,
                entry.Culture ?? string.Empty,
                entry.Value,
                entry.Source ?? string.Empty,
                entry.Comment ?? string.Empty,
                entry.Context ?? string.Empty,
                entry.State ?? string.Empty,
                entry.PluralCategory ?? string.Empty,
                entry.Format ?? string.Empty
            ], delimiter);
        }

        return builder.ToString();
    }

    private static IEnumerable<TranslationCatalogEntry> SelectEntries(TranslationCatalog catalog, string? culture)
    {
        IEnumerable<TranslationCatalogEntry> entries = catalog.Entries;
        if (!string.IsNullOrWhiteSpace(culture))
        {
            entries = entries.Where(entry => string.Equals(entry.Culture, culture, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(entry.Culture));
        }

        return entries
            .GroupBy(static entry => entry.Key, static entry => entry, StringComparer.Ordinal)
            .Select(group => SelectBestEntry(group, culture))
            .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
            .ThenBy(static entry => entry.Culture, StringComparer.OrdinalIgnoreCase);
    }

    private static TranslationCatalogEntry SelectBestEntry(IEnumerable<TranslationCatalogEntry> entries, string? culture)
    {
        TranslationCatalogEntry? neutral = null;
        TranslationCatalogEntry? fallback = null;

        foreach (TranslationCatalogEntry entry in entries)
        {
            fallback = entry;

            if (!string.IsNullOrWhiteSpace(culture)
                && string.Equals(entry.Culture, culture, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }

            if (string.IsNullOrWhiteSpace(entry.Culture))
            {
                neutral = entry;
            }
        }

        return neutral ?? fallback ?? throw new InvalidOperationException("Translation entry group was empty.");
    }

    private static string? InferCultureFromPath(string? path, string extension)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string fileName = Path.GetFileName(path);
        if (!fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string withoutExtension = fileName[..^extension.Length];
        int separator = withoutExtension.LastIndexOf('.');
        if (separator < 0 || separator == withoutExtension.Length - 1)
        {
            return null;
        }

        return TranslationCatalog.NormalizeCultureName(withoutExtension[(separator + 1)..]);
    }

    private static string? InferAndroidCulture(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string? directory = Path.GetFileName(Path.GetDirectoryName(path));
        if (directory is null || !directory.StartsWith("values-", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string qualifier = directory["values-".Length..].Replace("-r", "-", StringComparison.OrdinalIgnoreCase);
        return TranslationCatalog.NormalizeCultureName(qualifier);
    }

    private static string? InferAppleCulture(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string? directory = Path.GetFileName(Path.GetDirectoryName(path));
        if (directory is not null && directory.EndsWith(".lproj", StringComparison.OrdinalIgnoreCase))
        {
            return TranslationCatalog.NormalizeCultureName(directory[..^".lproj".Length]);
        }

        return InferCultureFromPath(path, Path.GetExtension(path));
    }

    private static string TrimPluralSuffix(TranslationCatalogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.PluralCategory))
        {
            return entry.Key;
        }

        string suffix = "." + entry.PluralCategory;
        return entry.Key.EndsWith(suffix, StringComparison.Ordinal)
            ? entry.Key[..^suffix.Length]
            : entry.Key;
    }

    private static Dictionary<string, XElement> ReadPlistDict(XElement dict)
    {
        var values = new Dictionary<string, XElement>(StringComparer.Ordinal);
        XElement? key = null;
        foreach (XElement child in dict.Elements())
        {
            if (child.Name.LocalName == "key")
            {
                key = child;
                continue;
            }

            if (key is not null)
            {
                values[key.Value] = child;
                key = null;
            }
        }

        return values;
    }

    private static void SkipWhitespaceAndAppleComments(string text, ref int index, ref string? comment)
    {
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                index++;
                continue;
            }

            if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '*')
            {
                int end = text.IndexOf("*/", index + 2, StringComparison.Ordinal);
                if (end < 0)
                {
                    index = text.Length;
                    return;
                }

                comment = text[(index + 2)..end].Trim();
                index = end + 2;
                continue;
            }

            if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '/')
            {
                int end = text.IndexOf('\n', index + 2);
                comment = text[(index + 2)..(end < 0 ? text.Length : end)].Trim();
                index = end < 0 ? text.Length : end + 1;
                continue;
            }

            break;
        }
    }

    private static bool TryReadAppleQuotedString(string text, ref int index, out string value)
    {
        value = string.Empty;
        if (index >= text.Length || text[index] != '"')
        {
            return false;
        }

        index++;
        var builder = new StringBuilder();
        while (index < text.Length)
        {
            char current = text[index++];
            if (current == '"')
            {
                value = builder.ToString();
                return true;
            }

            if (current == '\\' && index < text.Length)
            {
                char escaped = text[index++];
                builder.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '"' => '"',
                    '\\' => '\\',
                    _ => escaped
                });
            }
            else
            {
                builder.Append(current);
            }
        }

        return false;
    }

    private static string EscapeAppleString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }

    private static IEnumerable<PoEntry> ParsePoEntries(string content)
    {
        var entries = new List<PoEntry>();
        var current = new PoEntry();
        string? active = null;
        int? pluralIndex = null;

        foreach (string rawLine in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0)
            {
                if (current.HasContent)
                {
                    entries.Add(current);
                    current = new PoEntry();
                    active = null;
                    pluralIndex = null;
                }

                continue;
            }

            if (line.StartsWith("#.", StringComparison.Ordinal))
            {
                current.Comment = AppendComment(current.Comment, line[2..].Trim());
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("msgctxt", StringComparison.Ordinal))
            {
                current.Context = ReadPoString(line["msgctxt".Length..].Trim());
                active = "msgctxt";
                pluralIndex = null;
                continue;
            }

            if (line.StartsWith("msgid_plural", StringComparison.Ordinal))
            {
                current.MsgIdPlural = ReadPoString(line["msgid_plural".Length..].Trim());
                active = "msgid_plural";
                pluralIndex = null;
                continue;
            }

            if (line.StartsWith("msgid", StringComparison.Ordinal))
            {
                current.MsgId = ReadPoString(line["msgid".Length..].Trim());
                active = "msgid";
                pluralIndex = null;
                continue;
            }

            if (line.StartsWith("msgstr[", StringComparison.Ordinal))
            {
                int end = line.IndexOf(']', StringComparison.Ordinal);
                pluralIndex = int.Parse(line[7..end], CultureInfo.InvariantCulture);
                current.PluralTranslations[pluralIndex.Value] = ReadPoString(line[(end + 1)..].Trim());
                active = "msgstr";
                continue;
            }

            if (line.StartsWith("msgstr", StringComparison.Ordinal))
            {
                current.MsgStr = ReadPoString(line["msgstr".Length..].Trim());
                active = "msgstr";
                pluralIndex = null;
                continue;
            }

            if (line.StartsWith('"') && active is not null)
            {
                string fragment = ReadPoString(line);
                switch (active)
                {
                    case "msgctxt":
                        current.Context += fragment;
                        break;
                    case "msgid":
                        current.MsgId += fragment;
                        break;
                    case "msgid_plural":
                        current.MsgIdPlural += fragment;
                        break;
                    case "msgstr" when pluralIndex is not null:
                        current.PluralTranslations[pluralIndex.Value] += fragment;
                        break;
                    case "msgstr":
                        current.MsgStr += fragment;
                        break;
                }
            }
        }

        if (current.HasContent)
        {
            entries.Add(current);
        }

        return entries;
    }

    private static string? AppendComment(string? current, string next)
    {
        return string.IsNullOrWhiteSpace(current) ? next : current + Environment.NewLine + next;
    }

    private static string ReadPoHeaderLanguage(string header)
    {
        foreach (string line in header.Split('\n'))
        {
            if (line.StartsWith("Language:", StringComparison.OrdinalIgnoreCase))
            {
                return line["Language:".Length..].Trim();
            }
        }

        return string.Empty;
    }

    private static string ReadPoString(string token)
    {
        token = token.Trim();
        if (token.Length < 2 || token[0] != '"' || token[^1] != '"')
        {
            return string.Empty;
        }

        string inner = token[1..^1];
        var builder = new StringBuilder(inner.Length);
        for (int i = 0; i < inner.Length; i++)
        {
            char current = inner[i];
            if (current == '\\' && i + 1 < inner.Length)
            {
                char escaped = inner[++i];
                builder.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '"' => '"',
                    '\\' => '\\',
                    _ => escaped
                });
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }

    private static string EscapePo(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }

    private static List<string[]> ParseDelimitedRows(string content, char delimiter)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;

        for (int i = 0; i < content.Length; i++)
        {
            char current = content[i];
            if (quoted)
            {
                if (current == '"' && i + 1 < content.Length && content[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (current == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(current);
                }

                continue;
            }

            if (current == '"')
            {
                quoted = true;
            }
            else if (current == delimiter)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (current == '\r')
            {
            }
            else if (current == '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                rows.Add(row.ToArray());
                row.Clear();
            }
            else
            {
                field.Append(current);
            }
        }

        row.Add(field.ToString());
        if (row.Count > 1 || row[0].Length > 0)
        {
            rows.Add(row.ToArray());
        }

        return rows;
    }

    private static string GetColumn(string[] row, Dictionary<string, int> columns, string name, string? fallback = "")
    {
        return columns.TryGetValue(name, out int index) && index < row.Length ? row[index] : fallback ?? string.Empty;
    }

    private static void AppendDelimitedRow(StringBuilder builder, string[] fields, char delimiter)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(delimiter);
            }

            string field = fields[i];
            if (field.Contains(delimiter, StringComparison.Ordinal) || field.Contains('"', StringComparison.Ordinal) || field.Contains('\n', StringComparison.Ordinal))
            {
                builder.Append('"');
                builder.Append(field.Replace("\"", "\"\"", StringComparison.Ordinal));
                builder.Append('"');
            }
            else
            {
                builder.Append(field);
            }
        }

        builder.AppendLine();
    }

    private sealed class PoEntry
    {
        public string? Context { get; set; }

        public string MsgId { get; set; } = string.Empty;

        public string MsgIdPlural { get; set; } = string.Empty;

        public string MsgStr { get; set; } = string.Empty;

        public string? Comment { get; set; }

        public Dictionary<int, string> PluralTranslations { get; } = [];

        public bool HasContent => Context is not null || MsgId.Length > 0 || MsgStr.Length > 0 || PluralTranslations.Count > 0;
    }
}
