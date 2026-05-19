using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ProTranslate.Analyzers;

internal sealed class CatalogModel
{
    public CatalogModel(
        ImmutableDictionary<string, KeyEntry> keys,
        ImmutableDictionary<string, ImmutableHashSet<int>> placeholderCounts,
        ImmutableArray<Diagnostic> diagnostics)
    {
        Keys = keys;
        PlaceholderCounts = placeholderCounts;
        Diagnostics = diagnostics;
    }

    public ImmutableDictionary<string, KeyEntry> Keys { get; }

    public ImmutableDictionary<string, ImmutableHashSet<int>> PlaceholderCounts { get; }

    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public bool HasKeys => Keys.Count > 0;

    public static CatalogModel Create(ImmutableArray<AdditionalText> additionalFiles, System.Threading.CancellationToken cancellationToken)
    {
        var files = ImmutableArray.CreateBuilder<CatalogFile>();

        foreach (AdditionalText additionalFile in additionalFiles)
        {
            if (!CatalogFileNames.IsCatalogFile(additionalFile.Path))
            {
                continue;
            }

            SourceText? text = additionalFile.GetText(cancellationToken);
            if (text is null)
            {
                continue;
            }

            files.Add(CatalogParser.Parse(additionalFile.Path, text));
        }

        return Create(files.ToImmutable());
    }

    private static CatalogModel Create(ImmutableArray<CatalogFile> files)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var keys = new Dictionary<string, KeyEntry>(StringComparer.Ordinal);
        var placeholders = new Dictionary<string, ImmutableHashSet<int>.Builder>(StringComparer.Ordinal);

        foreach (CatalogFile file in files.OrderBy(static file => file.Path, StringComparer.OrdinalIgnoreCase))
        {
            diagnostics.AddRange(file.Diagnostics);

            foreach (KeyEntry entry in file.Keys)
            {
                if (!keys.ContainsKey(entry.Key))
                {
                    keys.Add(entry.Key, entry);
                }

                if (entry.Value is not null && FormatPlaceholderCounter.TryGetRequiredArgumentCount(entry.Value, out int count))
                {
                    if (!placeholders.TryGetValue(entry.Key, out ImmutableHashSet<int>.Builder? counts))
                    {
                        counts = ImmutableHashSet.CreateBuilder<int>();
                        placeholders.Add(entry.Key, counts);
                    }

                    counts.Add(count);
                }
            }
        }

        foreach (Diagnostic diagnostic in CreateCoverageDiagnostics(files))
        {
            diagnostics.Add(diagnostic);
        }

        return new CatalogModel(
            keys.ToImmutableDictionary(StringComparer.Ordinal),
            placeholders.ToImmutableDictionary(static pair => pair.Key, static pair => pair.Value.ToImmutable()),
            diagnostics.ToImmutable());
    }

    private static IEnumerable<Diagnostic> CreateCoverageDiagnostics(ImmutableArray<CatalogFile> files)
    {
        CatalogFile[] cultureCatalogs = files
            .Where(static file => file.CultureName is not null)
            .OrderBy(static file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (cultureCatalogs.Length == 0)
        {
            yield break;
        }

        string[] requiredKeys = files
            .SelectMany(static file => file.Keys.Select(static key => key.Key))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();

        foreach (CatalogFile cultureCatalog in cultureCatalogs)
        {
            var catalogKeys = cultureCatalog.Keys
                .Select(static key => key.Key)
                .ToImmutableHashSet(StringComparer.Ordinal);

            foreach (string requiredKey in requiredKeys)
            {
                if (catalogKeys.Contains(requiredKey))
                {
                    continue;
                }

                yield return Diagnostic.Create(
                    DiagnosticDescriptors.ResourceCoverageGap,
                    CreateFileLocation(cultureCatalog.Path, cultureCatalog.Text),
                    Path.GetFileName(cultureCatalog.Path),
                    requiredKey);
            }
        }
    }

    private static Location CreateFileLocation(string path, SourceText text)
    {
        var span = new TextSpan(0, 0);
        return Location.Create(path, span, text.Lines.GetLinePositionSpan(span));
    }
}

internal sealed class CatalogFile
{
    public CatalogFile(
        string path,
        SourceText text,
        string? cultureName,
        ImmutableArray<KeyEntry> keys,
        ImmutableArray<Diagnostic> diagnostics)
    {
        Path = path;
        Text = text;
        CultureName = cultureName;
        Keys = keys;
        Diagnostics = diagnostics;
    }

    public string Path { get; }

    public SourceText Text { get; }

    public string? CultureName { get; }

    public ImmutableArray<KeyEntry> Keys { get; }

    public ImmutableArray<Diagnostic> Diagnostics { get; }
}

internal sealed class KeyEntry
{
    public KeyEntry(string key, string? value, Location location)
    {
        Key = key;
        Value = value;
        Location = location;
    }

    public string Key { get; }

    public string? Value { get; }

    public Location Location { get; }
}

internal static class CatalogFileNames
{
    public static bool IsCatalogFile(string path)
    {
        return path.EndsWith(".protranslate.keys.txt", StringComparison.OrdinalIgnoreCase)
            || IsJsonCatalogFile(path);
    }

    public static bool IsJsonCatalogFile(string path)
    {
        string fileName = Path.GetFileName(path);
        return fileName.EndsWith(".protranslate.json", StringComparison.OrdinalIgnoreCase)
            || (fileName.StartsWith("Strings.", StringComparison.OrdinalIgnoreCase)
                && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
    }

    public static string? TryGetCultureName(string path)
    {
        string fileName = Path.GetFileName(path);
        string? candidate = null;

        if (fileName.StartsWith("Strings.", StringComparison.OrdinalIgnoreCase)
            && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            candidate = fileName.Substring("Strings.".Length, fileName.Length - "Strings.".Length - ".json".Length);
            if (candidate.EndsWith(".protranslate", StringComparison.OrdinalIgnoreCase))
            {
                candidate = candidate.Substring(0, candidate.Length - ".protranslate".Length);
            }
        }
        else if (fileName.EndsWith(".protranslate.json", StringComparison.OrdinalIgnoreCase))
        {
            candidate = fileName.Substring(0, fileName.Length - ".protranslate.json".Length);
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        try
        {
            _ = CultureInfo.GetCultureInfo(candidate);
            return candidate;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
