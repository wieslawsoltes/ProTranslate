---
title: API Surface
description: Current ProTranslate public API, XAML API, source generator output, analyzer diagnostics, and extension boundaries.
---

# API Surface

ProTranslate is service-first. The framework-neutral API lives in the `ProTranslate` namespace, and framework adapters expose native XAML integration in package-specific namespaces. The preview API is intentionally synchronous and deterministic so it works well in desktop and mobile XAML binding paths.

## Core Contracts

### `ICultureService`

`ICultureService` owns the active formatting culture, UI culture, region metadata, and text-flow metadata.

Key members:

```csharp
event EventHandler<CultureChangedEventArgs>? CultureChanged;
CultureInfo CurrentCulture { get; }
CultureInfo CurrentUICulture { get; }
RegionInfo CurrentRegion { get; }
bool IsMetric { get; }
TextFlowDirection FlowDirection { get; }
void SetCulture(CultureInfo culture);
void SetCulture(string cultureName);
void SetCulture(CultureInfo culture, CultureInfo uiCulture);
```

Use `CurrentCulture` for numeric, date, currency, and formatting decisions. Use `CurrentUICulture` for translation lookup. `CultureServiceOptions` controls whether selected cultures are applied to current and default thread cultures.

### `ITranslationProvider`

Providers resolve one key for one culture:

```csharp
public interface ITranslationProvider
{
    string Name { get; }
    LocalizedString GetString(string key, CultureInfo culture);
}
```

Implemented providers:

- `InMemoryTranslationProvider`.
- `CompositeTranslationProvider`.
- `ResourceManagerTranslationProvider`.
- `StringLocalizerTranslationProvider`.

Providers should return `LocalizedString` with `ResourceNotFound = true` for normal misses. Exceptions are reserved for provider failures.

### `ITranslationService`

`ITranslationService` applies fallback, diagnostics, formatting, and observable localized values:

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

`GetString` uses `CurrentUICulture`. `Format` uses `CurrentCulture` after lookup. `Observe` returns an `IObservableLocalizedString` that raises property changes when the culture changes.

### `IGlobalizationService`

`IGlobalizationService` combines culture, translations, region profile, measurement profile, and flow direction:

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

Use this service when a view model needs both translation and non-text globalization data.

`SetRegionOverride`, `ClearRegionOverride`, `SetMeasurementSystemOverride`, and `ClearMeasurementSystemOverride` raise `CultureChanged` when they change effective globalization metadata. The event carries the current cultures as both old and new values for metadata-only changes.

## Result And Diagnostic Types

`LocalizedString` carries:

- `Key`.
- `Value`.
- `Culture`.
- `ResourceNotFound`.
- `ProviderName`.
- `Diagnostics`.

Diagnostics use:

- `ProTranslateDiagnosticKind.MissingTranslation`.
- `ProTranslateDiagnosticKind.ProviderFailure`.
- `ProTranslateDiagnosticKind.FormatFailure`.
- `ProTranslateDiagnosticSeverity.Information`.
- `ProTranslateDiagnosticSeverity.Warning`.
- `ProTranslateDiagnosticSeverity.Error`.

Applications can listen to `ITranslationService.DiagnosticReported` or register an `IProTranslateDiagnosticSink`.

## Fallback Options

`TranslationFallbackOptions` controls:

- Parent-culture fallback.
- Explicit fallback cultures.
- Default culture fallback.
- Whether missing keys return the key or an empty string.
- Provider failure behavior.
- Format failure behavior.

The default behavior is production-tolerant: report diagnostics and keep rendering where possible.

## Cache And Culture Options

`TranslationCacheOptions` controls lookup caching:

```csharp
public sealed class TranslationCacheOptions
{
    public bool Enabled { get; set; }
    public bool CacheMissingTranslations { get; set; }
    public bool ClearOnCultureChanged { get; set; }
    public int MaximumEntries { get; set; }
}
```

`ITranslationCacheInvalidator` exposes `CachedLookupCount`, `ClearCache`, and `RemoveCachedString` for hosts that reload provider data.

`CultureServiceOptions` controls thread-culture mutation:

```csharp
public sealed class CultureServiceOptions
{
    public bool ApplyToDefaultThread { get; set; }
    public bool ApplyToCurrentThread { get; set; }
}
```

## XAML Adapter APIs

Each framework adapter exposes the same conceptual API:

- `TranslateExtension`.
- `TExtension`.
- `FormatExtension`.
- `FExtension`.
- `TranslationBindingSource`.
- Static adapter `TranslationService`.
- Attached `Translation.Key`.
- Attached `Translation.FallbackValue`.
- Attached `Translation.StringFormat`.
- Attached `Translation.Culture`.
- Attached `Translation.AutoFlowDirection`.
- `AddProTranslateAvalonia`, `AddProTranslateWpf`, `AddProTranslateMaui`, `AddProTranslateWinUI`, or `AddProTranslateUno`.
- `UseProTranslateAvalonia`, `UseProTranslateWpf`, `UseProTranslateMaui`, `UseProTranslateWinUI`, or `UseProTranslateUno` for post-build DI bootstrap.

Representative Avalonia or WPF syntax with default namespace mapping:

```xml
<TextBlock Text="{Translate Shell.Title}" />
<TextBlock Text="{T Shell.Subtitle}" />
<StackPanel Translation.Culture="{Binding CurrentCulture}"
            Translation.AutoFlowDirection="True" />
```

Portable explicit syntax:

```xml
<TextBlock xmlns:pt="https://github.com/protranslate/xaml"
           Text="{pt:Translate Shell.Title}" />
```

WinUI syntax:

```xml
<TextBlock xmlns:pt="using:ProTranslate.WinUI"
           Text="{pt:Translate Shell.Title}" />
```

## XML Namespace Support

| Framework | Recommended namespace | Prefix-free support |
| --- | --- | --- |
| Avalonia | Default Avalonia namespace or `https://github.com/protranslate/xaml` | Yes, through Avalonia `XmlnsDefinition` into `https://github.com/avaloniaui`. |
| WPF | Default WPF presentation namespace or `https://github.com/protranslate/xaml` | Yes, through WPF `XmlnsDefinition` into the presentation namespace. |
| .NET MAUI | `https://github.com/protranslate/xaml` | No. MAUI protects its default namespace from third-party additions. |
| WinUI | `using:ProTranslate.WinUI` | No supported assembly-level `XmlnsDefinitionAttribute`. |
| Uno Platform | `using:ProTranslate.Uno` or shared URI where Uno tooling honors it | Prefix-free default namespace should not be assumed. |

See [XAML Namespaces](xaml-namespaces.md) for framework-specific examples.

## Formatting API

Core:

```csharp
string value = translations.Format("Orders.Total", total);
```

XAML adapters:

```xml
<TextBlock Text="{Format Orders.Total, Value={Binding Total}}" />
```

Avalonia, WPF, and MAUI can use framework multi-binding support for bound format values. WinUI and Uno should prefer `x:Bind` or a view-model property for dynamic formatted values.

Unit conversion and localized unit formatting:

```csharp
double miles = globalization.UnitConverter.Convert(
    42.2,
    MeasurementUnit.Kilometer,
    MeasurementUnit.Mile);

string text = globalization.UnitFormatter.Format(
    miles,
    MeasurementUnit.Mile,
    globalization.CurrentCulture,
    "N1");
```

## Source Generator Output

Inputs:

- `*.protranslate.keys.txt`.
- `Strings.*.json`.
- `*.protranslate.json`.
- normalized ProTranslate catalogs produced by import/export tooling.

Output namespace:

```csharp
namespace ProTranslate.Generated;
```

Generated classes:

```csharp
public static partial class ProTranslateKeys
{
    public const string ShellTitle = "Shell.Title";
}

public static partial class ProTranslateAccessors
{
    public static LocalizedString Get_ShellTitle(this ITranslationService translations);
    public static string Value_ShellTitle(this ITranslationService translations);
    public static string Format_ShellTitle(this ITranslationService translations, params object?[] arguments);
    public static IObservableLocalizedString Observe_ShellTitle(this ITranslationService translations, params object?[] arguments);
}

public static partial class ProTranslateProviderManifest
{
    public static ReadOnlySpan<ProTranslateProviderManifestEntry> Entries { get; }
    public static ReadOnlySpan<string> Keys { get; }
    public static ReadOnlySpan<string> Cultures { get; }
    public static ReadOnlySpan<string> SourceFiles { get; }
}
```

Diagnostics:

- `PTSG001`: invalid ProTranslate JSON catalog.
- `PTSG002`: duplicate ProTranslate key in a catalog file.

## Import/Export Tooling

This is optional tooling surface, not runtime lookup API. It supports XLIFF 1.2/2.1 workflows, gettext PO/POT, RESX, Android `strings.xml`, Apple `.strings`/`.stringsdict`/`.xcstrings`, Flutter ARB, i18next JSON, and CSV/TSV exchange.

The intended API boundary is:

- import external files into normalized ProTranslate catalogs
- report loss-aware diagnostics for unsupported constructs
- export normalized catalogs back to exchange or platform formats
- produce source-generator-ready catalogs for application runtime

XLIFF is preferred for CAT/TMS exchange. Source-generated ProTranslate catalogs are preferred for runtime lookup. Import exchange files through `ProTranslate.Formats` or authoring tools first, then feed normalized ProTranslate catalogs to the source generator.

## Analyzer Diagnostics

`ProTranslate.Analyzers` reports:

- `PTA001`: missing static key.
- `PTA002`: placeholder count mismatch.
- `PTA003`: resource coverage gap.
- `PTA004`: unsafe dynamic key.
- `PTA005`: invalid catalog.

## Extension Boundaries

The following remain extension or future hardening areas:

- Analyzer code fixes.
- Cache hit/miss diagnostics and richer provider traces.
- Logging bridge and debug overlay APIs.
- Weak target registry APIs for adapter target retention diagnostics.
- Broader import/export round-trip validation and Uno Translation Studio UI smoke coverage.

The deeper working specification remains in [docs/spec/api-surface.md](https://github.com/wieslawsoltes/ProTranslate/blob/main/docs/spec/api-surface.md).
