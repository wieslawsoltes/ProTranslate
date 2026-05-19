---
title: Source Generator
description: Generate strongly typed keys, bindable strings, provider code, and translation accessors from text and JSON catalogs.
---

# Source Generator

`ProTranslate.SourceGenerator` provides compile-time key safety and compiled-binding-friendly CLR surfaces without moving culture policy out of the core runtime. It is an incremental Roslyn generator that reads translation catalog files from `AdditionalFiles` and emits framework-neutral constants, extension methods for `ITranslationService`, a bindable `ProTranslateStrings` class, a generated JSON catalog provider, and a deterministic provider manifest.

The generator does not replace the provider pipeline. Runtime lookup, fallback, formatting, culture switching, caching, and diagnostics still belong to `ProTranslate.Core`. The generated provider is an optional provider implementation for JSON catalogs when you want a reflection-free static catalog path.

Industry format import/export lives in tooling around normalized ProTranslate catalogs. The preferred generator input is source-generator-ready ProTranslate JSON or key files. Use XLIFF for CAT/TMS exchange, review loss diagnostics in tooling, then commit generated ProTranslate catalogs for app runtime.

## Add Catalog Files

Add source catalogs as MSBuild `AdditionalFiles`:

```xml
<ItemGroup>
  <AdditionalFiles Include="Resources\Strings.*.json" />
  <AdditionalFiles Include="Resources\*.protranslate.json" />
  <AdditionalFiles Include="Resources\*.protranslate.keys.txt" />
</ItemGroup>
```

The generator recognizes these file names:

| Format | File names | Behavior |
| --- | --- | --- |
| Line-delimited keys | `*.protranslate.keys.txt` | Each non-empty trimmed line is a key. Lines starting with `#` are comments. |
| Culture JSON catalogs | `Strings.*.json` | The file must have a JSON object root. String-valued properties become keys. Nested objects are flattened with dot separators. |
| ProTranslate JSON catalogs | `*.protranslate.json` | Same JSON extraction rules as `Strings.*.json`. |

Example text catalog:

```text
# Shell
Shell.Title
Shell.FileMenu
Orders.EmptyState
Orders.Total
```

Example JSON catalog:

```json
{
  "Shell": {
    "Title": "ProTranslate",
    "FileMenu": "File"
  },
  "Orders.EmptyState": "No orders",
  "Orders": {
    "Total": "Total: {0:C}"
  }
}
```

This JSON contributes `Shell.Title`, `Shell.FileMenu`, `Orders.EmptyState`, and `Orders.Total`. Non-string JSON values are ignored for key generation.

## Generated API

When at least one key is found, the generator emits source in the `ProTranslate.Generated` namespace.

`ProTranslateKeys.g.cs` contains key constants and `ITranslationService` accessors:

```csharp
namespace ProTranslate.Generated;

public static partial class ProTranslateKeys
{
    public const string OrdersTotal = "Orders.Total";
    public const string ShellTitle = "Shell.Title";
}

public static partial class ProTranslateAccessors
{
    public static ProTranslate.LocalizedString Get_OrdersTotal(
        this ProTranslate.ITranslationService translations) =>
        translations.GetString(ProTranslateKeys.OrdersTotal);

    public static string Value_OrdersTotal(
        this ProTranslate.ITranslationService translations) =>
        translations.GetString(ProTranslateKeys.OrdersTotal).Value;

    public static string Format_OrdersTotal(
        this ProTranslate.ITranslationService translations,
        params object?[] arguments) =>
        translations.Format(ProTranslateKeys.OrdersTotal, arguments);

    public static ProTranslate.IObservableLocalizedString Observe_OrdersTotal(
        this ProTranslate.ITranslationService translations,
        params object?[] arguments) =>
        translations.Observe(ProTranslateKeys.OrdersTotal, arguments);
}
```

Identifiers are deterministic. Letters and digits are preserved, separators such as `.` and `-` start a new PascalCase segment, leading digits are prefixed with `_`, C# keywords are prefixed with `_`, and normalized-name collisions receive a stable hash suffix.

`ProTranslateStrings.g.cs` contains a bindable CLR property surface for compiled bindings and `x:Bind`:

```csharp
namespace ProTranslate.Generated;

public sealed partial class ProTranslateStrings : INotifyPropertyChanged, IDisposable
{
    public ProTranslateStrings(ITranslationService translations);

    public CultureInfo Culture { get; }
    public CultureInfo UICulture { get; }
    public string ShellTitle { get; }
    public string OrdersTotal { get; }

    public LocalizedString Get_ShellTitle();
    public string Format_OrdersTotal(params object?[] arguments);
    public IObservableLocalizedString Observe_OrdersTotal(params object?[] arguments);
    public void Refresh();
}
```

`ProTranslateStrings` subscribes to `ITranslationService.CultureChanged` and raises `PropertyChanged` for every generated property. Dispose it when the owner view model or service scope is disposed.

`ProTranslateGeneratedTranslationProvider.g.cs` contains an `ITranslationProvider` implementation for string values found in JSON catalogs:

```csharp
var provider = new ProTranslateGeneratedTranslationProvider("AppCatalog");
var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
var translations = new TranslationService(provider, cultures);
var strings = new ProTranslateStrings(translations);
```

`Strings.en-US.json` entries are emitted with culture `en-US`. `*.protranslate.json` entries are emitted as culture-neutral provider entries and can satisfy any culture after the core fallback pipeline asks the provider for a value. Text key catalogs contribute generated keys and manifests only because they do not contain localized values.

## Provider Manifest

The generator also emits `ProTranslateProviderManifest.g.cs`. The manifest gives packaging, diagnostics, and host tooling a deterministic view of catalog contents without loading provider resources at runtime:

```csharp
using ProTranslate.Generated;

foreach (ProTranslateProviderManifestEntry entry in ProTranslateProviderManifest.Entries)
{
    string key = entry.Key;
    string? culture = entry.Culture;
    string sourceFile = entry.SourceFile;
    ReadOnlySpan<int> placeholders = entry.PlaceholderIndexes;
}
```

Manifest entries are sorted by key, inferred culture, source file, and placeholder indexes. JSON string values contribute placeholder indexes such as `0` and `1` from composite-format strings. Text key catalogs contribute keys and source files without culture or placeholder metadata.

## Use With Services

Use generated constants when a normal runtime lookup is enough:

```csharp
using ProTranslate.Generated;

LocalizedString title = translations.GetString(ProTranslateKeys.ShellTitle);
string total = translations.Format_OrdersTotal(order.Total);
```

Use generated observable accessors when a view model needs a single binding-friendly value that refreshes on culture changes:

```csharp
using ProTranslate.Generated;

public sealed class OrderSummaryViewModel : IDisposable
{
    private readonly IObservableLocalizedString _totalText;

    public OrderSummaryViewModel(ITranslationService translations, decimal total)
    {
        _totalText = translations.Observe_OrdersTotal(total);
    }

    public string TotalText => _totalText.Value;

    public void Dispose() => _totalText.Dispose();
}
```

`IObservableLocalizedString` subscribes to `ITranslationService.CultureChanged`, so long-lived view models should dispose observable strings when the view model is no longer used.

## Compiled Binding And `x:Bind`

Compiled binding and WinUI/Uno `x:Bind` work best with strongly typed CLR members. Prefer `ProTranslateStrings` or a view-model property that wraps generated accessors:

```csharp
using ProTranslate.Generated;

public sealed class ShellViewModel : IDisposable
{
    public ShellViewModel(ITranslationService translations)
    {
        Strings = new ProTranslateStrings(translations);
    }

    public ProTranslateStrings Strings { get; }

    public string FormattedTotal => Strings.Format_OrdersTotal(42m);

    public void Dispose() => Strings.Dispose();
}
```

Bind to normal CLR members:

```xml
<TextBlock Text="{Binding Strings.ShellTitle}" />
<TextBlock Text="{x:Bind ViewModel.Strings.ShellTitle, Mode=OneWay}" />
```

Avalonia apps should enable compiled bindings and set `x:DataType`. WinUI and Uno apps should keep static and formatted translation output on `x:Bind`-safe view-model members. Adapter markup extensions still work for concise view-only XAML, but they are runtime binding paths and should be treated as convenience or migration APIs in trimming-sensitive applications.

## Diagnostics

The source generator reports two build warnings:

| ID | Severity | Meaning |
| --- | --- | --- |
| `PTSG001` | Warning | A JSON catalog could not be parsed as a ProTranslate object catalog. |
| `PTSG002` | Warning | The same key appears more than once inside one catalog file. |

`PTSG001` is raised for invalid JSON syntax or a non-object root, such as an array. `PTSG002` is scoped to duplicates within one additional file; declaring the same key in multiple culture files is expected and merged into one generated key.

Use `ProTranslate.Analyzers` beside the generator when you want missing-key, placeholder-mismatch, resource-coverage, unsafe-dynamic-key, and invalid-catalog diagnostics over consuming application code.

## Engineering Rules

- Treat generated keys as API: review renames the same way you review public property renames.
- Keep catalog files deterministic and source-controlled.
- Prefer normalized ProTranslate JSON for runtime catalogs; import XLIFF, PO/POT, RESX, Android, Apple, ARB, i18next JSON, and CSV/TSV through format tooling before committing generator inputs.
- Prefer generated constants/accessors for static keys and reserve dynamic keys for cases that cannot be known at compile time.
- Keep formatting placeholders consistent across cultures; the generator records placeholder indexes in the provider manifest and the analyzer validates static `Format` calls.
- Use `ProTranslateGeneratedTranslationProvider` or explicit provider registration when you want reflection-free catalog loading.
- Keep runtime fallback and provider ordering in `ProTranslate.Core`; generated provider code supplies values but does not encode application fallback policy.
