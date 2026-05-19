# ProTranslate API Surface Specification

## Status

This document describes the current preview API surface and separates it from remaining hardening work. Public C# APIs use the `ProTranslate` namespace for framework-neutral services and package-specific namespaces for adapters. XAML APIs use framework-appropriate XML namespace mappings, including the shared `https://github.com/protranslate/xaml` URI where the target XAML stack supports `XmlnsDefinition`.

## Implemented Core Contracts

### `ICultureService`

Inputs:
- `CultureInfo culture`
- optional `CultureInfo uiCulture`
- culture name string

Outputs:
- `CurrentCulture`
- `CurrentUICulture`
- `CurrentRegion`
- `IsMetric`
- `FlowDirection`
- `CultureChanged` event with old and new formatting/UI cultures

Constraints:
- no UI framework dependency
- same culture/UI culture switch is idempotent
- current/default thread culture mutation is controlled by `CultureServiceOptions`

Edge cases:
- invalid culture names are reported by .NET culture APIs
- neutral cultures are converted to specific cultures when creating `RegionInfo`
- invariant culture falls back to `RegionInfo.CurrentRegion`

Validate:
- culture and UI culture event payloads
- same-culture idempotence
- RTL culture flow direction
- region and metric metadata

### `ITranslationProvider`

Implemented shape:

```csharp
public interface ITranslationProvider
{
    string Name { get; }
    LocalizedString GetString(string key, CultureInfo culture);
}
```

Implemented providers:
- `InMemoryTranslationProvider`
- `CompositeTranslationProvider`
- `ResourceManagerTranslationProvider`
- `StringLocalizerTranslationProvider`

Inputs:
- key
- lookup culture

Outputs:
- `LocalizedString` with `Key`, `Value`, `Culture`, `ResourceNotFound`, `ProviderName`, and `Diagnostics`

Constraints:
- normal missing-key behavior returns a `LocalizedString`, not an exception
- provider order in `CompositeTranslationProvider` is deterministic
- provider exceptions can be reported and continued or reported and thrown
- provider traces beyond structured failure diagnostics remain hardening work

Edge cases:
- missing key
- empty string value
- provider fallback performed by `TranslationService`
- `ResourceManager` missing manifest or satellite assembly

Validate:
- provider priority
- parent/default fallback
- missing key state

### `ITranslationService`

Implemented shape:

```csharp
public interface ITranslationService
{
    event EventHandler<CultureChangedEventArgs>? CultureChanged;
    event EventHandler<ProTranslateDiagnosticEventArgs>? DiagnosticReported;
    CultureInfo CurrentCulture { get; }
    CultureInfo CurrentUICulture { get; }
    LocalizedString this[string key] { get; }
    LocalizedString GetString(string key);
    LocalizedString GetString(string key, CultureInfo culture);
    string Format(string key, params object?[] arguments);
    IObservableLocalizedString Observe(string key, params object?[] arguments);
}
```

Inputs:
- key
- optional explicit culture
- optional format arguments

Outputs:
- localized value
- formatted localized value through `string.Format(CurrentCulture, value, args)`
- observable localized string for binding-friendly view-model use
- structured diagnostics for missing keys, provider failures, and format failures

Constraints:
- fallback is controlled by `TranslationFallbackOptions`
- provider and format failure behavior is controlled by `TranslationProviderFailureBehavior` and `TranslationFormatFailureBehavior`
- async lookup is not part of the preview API

Edge cases:
- missing key returns the key by default
- `ReturnKeyWhenMissing = false` returns an empty value
- invalid format strings are reported and return the unformatted localized value by default, or can be reported and thrown

Validate:
- parent culture fallback
- configured default/fallback cultures
- observable value refresh after culture changes
- diagnostic event and sink reporting

### `IGlobalizationService`

Implemented shape:

```csharp
public interface IGlobalizationService
{
    event EventHandler<CultureChangedEventArgs>? CultureChanged;
    ICultureService Cultures { get; }
    ITranslationService Translations { get; }
    IUnitConversionService UnitConverter { get; }
    ILocalizedUnitFormatter UnitFormatter { get; }
    CultureInfo CurrentCulture { get; }
    bool IsMetric { get; }
    RegionProfile RegionProfile { get; }
    MeasurementSystem MeasurementSystem { get; }
    MeasurementSystemProfile MeasurementSystemProfile { get; }
    TextFlowDirection FlowDirection { get; }
    void SetCulture(CultureInfo culture);
    void SetRegionOverride(RegionInfo region);
    void ClearRegionOverride();
    void SetMeasurementSystemOverride(MeasurementSystem measurementSystem);
    void ClearMeasurementSystemOverride();
    LocalizedString GetString(string key);
    MeasurementValue ConvertMeasurement(double value, MeasurementUnit sourceUnit);
    string FormatMeasurement(double value, MeasurementUnit unit, string? format = null);
    string FormatMeasurementForProfile(double value, MeasurementUnit sourceUnit, string? format = null);
}
```

Measurement mapping:
- `US`, `LR`, `MM` -> `USCustomary`
- `GB` -> `Imperial`
- metric regions -> `Metric`
- remaining regions -> `Custom`

Region and measurement override methods raise `CultureChanged` when effective metadata changes. For metadata-only changes, old and new culture values in the event payload are the current formatting and UI cultures.

Implemented reusable services:
- `IRegionProfileProvider`
- `DefaultRegionProfileProvider`
- `IMeasurementSystemResolver`
- `DefaultMeasurementSystemResolver`
- `IUnitConversionService`
- `DefaultUnitConversionService`
- `ILocalizedUnitFormatter`
- `DefaultLocalizedUnitFormatter`
- `RegionProfile`
- `MeasurementSystemProfile`

Planned:
- unit preferences by quantity type
- user preference persistence and override policy

## Implemented Adapter APIs

Each adapter exposes the same preview concepts using native framework types:

- `TranslateExtension`
- `TExtension`
- `FormatExtension`
- `FExtension`
- `TranslationBindingSource`
- static `TranslationService`
- attached `Translation.Key`
- attached `Translation.FallbackValue`
- attached `Translation.StringFormat`
- attached `Translation.Culture`
- attached `Translation.AutoFlowDirection`
- `AddProTranslate{Framework}` DI helper

XML namespace support:
- Avalonia maps `ProTranslate.Avalonia` to `https://github.com/protranslate/xaml` and to Avalonia's default `https://github.com/avaloniaui` namespace.
- WPF maps `ProTranslate.Wpf` to `https://github.com/protranslate/xaml` and to WPF's default presentation namespace.
- MAUI maps `ProTranslate.Maui` to `https://github.com/protranslate/xaml`; MAUI rejects third-party additions to its protected default namespace, so prefixless default-namespace usage is not supported.
- Uno maps `ProTranslate.Uno` to `https://github.com/protranslate/xaml` for Uno tooling that honors `XmlnsDefinition`; the sample keeps WinUI-compatible `using:` syntax.
- WinUI does not expose a supported assembly-level `XmlnsDefinitionAttribute`; use `using:ProTranslate.WinUI`.

Representative XAML:

```xml
<TextBlock Text="{Translate AppTitle}" />
<Grid Translation.Culture="{Binding Strings.Culture}"
      Translation.AutoFlowDirection="True" />
```

Portable explicit form:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:pt="https://github.com/protranslate/xaml">
  <VerticalStackLayout>
    <Label Text="{pt:T AppTitle}" />
    <Label Text="{pt:F Orders.Total, Value={Binding Total}}" />
  </VerticalStackLayout>
</ContentPage>
```

Avalonia, WPF, and MAUI `FormatExtension` support a bound `Value` through native multi-binding. WinUI and Uno expose static value formatting through their adapter markup extensions and keep dynamic formatting in `x:Bind`/view-model paths where native XAML multi-binding is unavailable.

WinUI and Uno samples use `x:Bind` for strongly typed view-model properties and explicit `pt:` syntax for adapter markup-extension coverage.

Constraints:
- adapters reference only their framework, `ProTranslate.Abstractions`, and `ProTranslate.Core`
- adapters delegate lookup and formatting to the core services
- dispatcher abstractions and weak target registries remain hardening work

Edge cases:
- binding source is refreshed through `INotifyPropertyChanged` for `Culture` and `Item[]`
- attached key, fallback, and string-format values refresh on culture changes
- unloaded target leak behavior is validated for current Avalonia runtime paths and remains hardening work for other adapters
- WinUI and Uno real XAML sample paths are Windows CI validated

Validate:
- Avalonia adapter smoke tests for binding creation and refresh
- framework sample builds
- manual UI verification for runtime culture switching

## Implemented Source Generator

`ProTranslate.SourceGenerator` is an incremental generator that reads additional files named `*.protranslate.keys.txt`, `Strings.*.json`, and `*.protranslate.json`, then emits:

```csharp
namespace ProTranslate.Generated;

public static partial class ProTranslateKeys
{
    public const string AppTitle = "AppTitle";
}

public static partial class ProTranslateAccessors
{
    public static LocalizedString Get_AppTitle(this ITranslationService translations);
    public static string Value_AppTitle(this ITranslationService translations);
    public static string Format_AppTitle(this ITranslationService translations, params object?[] arguments);
    public static IObservableLocalizedString Observe_AppTitle(this ITranslationService translations, params object?[] arguments);
}

public sealed partial class ProTranslateStrings : INotifyPropertyChanged, IDisposable
{
    public ProTranslateStrings(ITranslationService translations);
    public CultureInfo Culture { get; }
    public CultureInfo UICulture { get; }
    public string AppTitle { get; }
    public LocalizedString Get_AppTitle();
    public string Format_AppTitle(params object?[] arguments);
    public IObservableLocalizedString Observe_AppTitle(params object?[] arguments);
    public void Refresh();
}

public sealed partial class ProTranslateGeneratedTranslationProvider : ITranslationProvider
{
    public ProTranslateGeneratedTranslationProvider();
    public ProTranslateGeneratedTranslationProvider(string name);
}

public static partial class ProTranslateProviderManifest
{
    public static ReadOnlySpan<ProTranslateProviderManifestEntry> Entries { get; }
    public static ReadOnlySpan<string> Keys { get; }
    public static ReadOnlySpan<string> Cultures { get; }
    public static ReadOnlySpan<string> SourceFiles { get; }
}

public readonly partial struct ProTranslateProviderManifestEntry
{
    public string Key { get; }
    public string? Culture { get; }
    public string SourceFile { get; }
    public ReadOnlySpan<int> PlaceholderIndexes { get; }
}
```

Inputs:
- line-delimited key files
- comments starting with `#`
- flat or nested JSON catalog objects
- normalized ProTranslate catalogs produced by import/export tooling

Outputs:
- deterministic constants sorted by key
- generated get/value/format/observe accessor helpers
- deterministic provider manifest entries sorted by key, inferred culture, source file, and placeholder indexes
- manifest culture inference from `Strings.<culture>.json`
- manifest source file lists and distinct culture lists
- basic numeric placeholder index extraction from JSON string values, such as `{0}`, `{1:C}`, and `{2,4:N0}`
- collision disambiguation with stable hash suffixes
- `PTSG001` warning for invalid JSON catalogs
- `PTSG002` warning for duplicate keys in one catalog file

Constraints:
- generated manifest types live in `ProTranslate.Generated` and use static arrays exposed as `ReadOnlySpan<T>` for trim-friendly runtime or tooling consumption
- invalid JSON files report diagnostics and do not contribute entries unless the parser already reached valid string values before the invalid token
- generator diagnostics are build warnings; `ProTranslate.Analyzers` provides consuming-code validation

Validate:
- identifier collision test
- JSON catalog extraction test
- deterministic manifest content test
- manifest culture inference test
- manifest placeholder extraction test
- duplicate and invalid JSON diagnostic tests
- sample or consuming project compile check

## Import/Export Tooling Surface

This surface belongs to optional tooling and the professional Uno Translation Studio app. It must not be treated as runtime lookup behavior in `ProTranslate.Core` or the XAML adapters.

Supported format targets:
- XLIFF 1.2 and 2.1
- gettext PO and POT
- .NET RESX
- Android `strings.xml`
- Apple `.strings`, `.stringsdict`, and `.xcstrings`
- Flutter ARB
- i18next JSON
- CSV and TSV

Inputs:
- external translation files
- normalized ProTranslate catalog records
- source and target cultures
- key namespace or resource scope
- comments, context, workflow state, placeholders, plural metadata, and source references
- explicit loss policy for export

Outputs:
- normalized ProTranslate catalogs suitable for source generation
- export files in the selected format
- structured import/export diagnostics
- conflict and review state metadata for authoring tools

Constraints:
- XLIFF is preferred for CAT/TMS exchange
- generated ProTranslate catalogs are preferred for runtime application lookup
- format parsers and writers remain framework-neutral
- Uno app UI depends on tooling services, not the other way around
- unsupported constructs require diagnostics
- source generator integration should consume normalized catalogs rather than adding broad exchange-format parsing directly to app builds

Edge cases:
- plural syntax mismatch between gettext, Android, Apple, ARB, i18next, and .NET formatting
- placeholder syntax mismatch between ICU, printf, XLIFF inline codes, i18next interpolation, Android XML, and .NET composite formatting
- non-string RESX resources
- styled or attributed platform strings
- CSV/TSV delimiter, encoding, formula, and duplicate-key issues

Validate:
- parser/writer tests
- round-trip tests
- diagnostics tests for lossy constructs
- source-generator compile tests from imported catalogs
- analyzer diagnostics over imported catalogs
- Uno authoring app UI smoke tests

## Dependency Injection

Implemented registration:

```csharp
services.AddProTranslate(
    provider: provider,
    culture: CultureInfo.GetCultureInfo("en-US"),
    cultureOptions: new CultureServiceOptions(),
    cacheOptions: new TranslationCacheOptions());

services.AddProTranslateStringLocalizer<AppResources>();
services.AddProTranslateAvalonia();
services.AddProTranslateWpf();
services.AddProTranslateMaui();
services.AddProTranslateWinUI();
services.AddProTranslateUno();
```

Constraints:
- `ProTranslate.MicrosoftExtensions` owns Microsoft.Extensions dependencies
- adapter DI helpers connect the static adapter binding source to the registered core services
- fluent `UseResourceManager`, `UseStringLocalizer`, and options-builder APIs remain future ergonomics work

## Analyzer Package

`ProTranslate.Analyzers` reports:

- `PTA001`: missing static key.
- `PTA002`: placeholder count mismatch.
- `PTA003`: resource coverage gap.
- `PTA004`: unsafe dynamic key.
- `PTA005`: invalid catalog.

## Remaining Hardening Work

The following remain design targets and must not be documented as shipped behavior until implemented:

- analyzer code fixes
- rich provider trace and cache hit/miss diagnostics
- logging/debug-overlay integrations over structured diagnostics
- broader runtime UI automation outside Avalonia
- broader import/export round-trip validation and Uno Translation Studio UI smoke coverage
