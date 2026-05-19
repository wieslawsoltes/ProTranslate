# ProTranslate

[![Docs](https://img.shields.io/badge/docs-github%20pages-0f766e)](https://wieslawsoltes.github.io/ProTranslate/)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![XAML](https://img.shields.io/badge/XAML-Avalonia%20%7C%20WPF%20%7C%20MAUI%20%7C%20WinUI%20%7C%20Uno-0F172A)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Preview translation and globalization infrastructure for XAML applications.

ProTranslate is organized around a framework-neutral core plus thin adapters for Avalonia, WPF, .NET MAUI, WinUI, and Uno. The core owns culture state, translation provider composition, formatting, `RegionProfile`-driven measurement-system defaults, fallback behavior, diagnostics, and flow-direction decisions. UI packages adapt those services into each framework's XAML property system through markup extensions, attached properties, bindable/dependency properties, and binding refresh paths.

The framework is intended for MVVM and SOLID application architectures: view models can depend on small abstractions when they own text-producing behavior, while view-only labels and formatting stay in XAML. Adapters avoid framework cross-dependencies and keep the reusable globalization model in `ProTranslate.Core`.

Documentation site: [wieslawsoltes.github.io/ProTranslate](https://wieslawsoltes.github.io/ProTranslate/)

## Current Status

This repository is a preview implementation. The implemented surface is intentionally small and service-first:

- `ProTranslate.Abstractions` and `ProTranslate.Core` provide `ICultureService`, `ITranslationService`, `IGlobalizationService`, culture-change events, parent/default fallback, cache policy options, simple formatting through `string.Format`, observable localized strings, structured diagnostics, `RegionProfile`, region and measurement-system overrides, measurement-system mapping, built-in unit conversion, localized unit formatting, and RTL/LTR resolution from `CultureInfo.TextInfo`.
- `ProTranslate.ResourceManager` and `ProTranslate.MicrosoftExtensions` provide `ResourceManager` and `IStringLocalizer` provider bridges.
- Avalonia, WPF, MAUI, WinUI, and Uno adapters provide `TranslateExtension`/`TExtension`, `FormatExtension`/`FExtension`, a binding source, attached `Translation.Key`, `Translation.FallbackValue`, `Translation.StringFormat`, `Translation.Culture`, `Translation.AutoFlowDirection`, XML namespace mappings where the framework supports them, and DI helpers.
- `ProTranslate.SourceGenerator` emits key constants, translation accessor helpers, a bindable `ProTranslateStrings` CLR surface, a source-generated JSON catalog provider, and a deterministic provider manifest from `*.protranslate.keys.txt`, `Strings.*.json`, and `*.protranslate.json` additional files.
- `ProTranslate.Analyzers` reports missing static keys, placeholder mismatches, catalog coverage gaps, unsafe dynamic keys, and invalid catalog files at build time.
- `ProTranslate.Formats` provides optional loss-aware import/export services for industry translation file formats and normalized ProTranslate catalogs.

Missing keys and provider/format failures are represented through `LocalizedString.ResourceNotFound`, `ProviderName`, `LocalizedString.Diagnostics`, `ITranslationService.DiagnosticReported`, and optional diagnostic sinks. `CultureServiceOptions` controls whether ProTranslate applies selected cultures to `Thread.CurrentThread` and `CultureInfo.DefaultThreadCurrentCulture`/`DefaultThreadCurrentUICulture`.

Translation format tooling targets XLIFF 1.2/2.1 workflows, gettext PO/POT, .NET RESX, Android `strings.xml`, Apple `.strings`/`.stringsdict`/`.xcstrings`, Flutter ARB, i18next JSON, and CSV/TSV exchange. XLIFF is the preferred CAT/TMS exchange format, while source-generated ProTranslate catalogs remain the preferred application runtime format. Import/export must be loss-aware and report diagnostics for unsupported constructs instead of silently dropping metadata.

## Packages

| Package | Purpose |
| --- | --- |
| `ProTranslate.Abstractions` | Framework-neutral interfaces, result types, culture events, measurement and flow-direction enums. |
| `ProTranslate.Core` | Framework-neutral culture manager, translation provider pipeline, formatting, cache policy, unit conversion, measurement system resolution, and flow-direction resolution. |
| `ProTranslate.ResourceManager` | `ResourceManager` provider for `.resx` and satellite assembly workflows. |
| `ProTranslate.MicrosoftExtensions` | Optional Microsoft.Extensions integration for dependency injection and `IStringLocalizer` provider registration. |
| `ProTranslate.Avalonia` | Avalonia `T`/`Translate` markup extensions, attached culture and flow-direction properties, binding refresh, XML namespace mappings, and DI helper. |
| `ProTranslate.Wpf` | WPF markup extensions, dependency properties, binding refresh, `XmlLanguage`, `FlowDirection`, and XML namespace mappings. |
| `ProTranslate.Maui` | .NET MAUI markup extensions, bindable attached properties, binding refresh, flow-direction integration, and the shared ProTranslate XAML URI. |
| `ProTranslate.WinUI` | WinUI markup extensions, dependency properties, `x:Bind`-friendly sample usage, and flow-direction integration. |
| `ProTranslate.Uno` | Uno adapter aligned with WinUI XAML binding, attached-property conventions, and the shared ProTranslate XAML URI where supported by Uno tooling. |
| `ProTranslate.SourceGenerator` | Strongly typed key constants, translation accessor generation, compiled-binding-friendly `ProTranslateStrings`, generated JSON catalog provider code, and provider manifest generation from text and JSON catalog inputs. |
| `ProTranslate.Analyzers` | Roslyn diagnostics for translation keys, placeholder consistency, and catalog coverage. |
| `ProTranslate.Formats` | Optional translation file import/export for XLIFF, PO/POT, RESX, Android, Apple, ARB, i18next JSON, CSV/TSV, and normalized ProTranslate catalogs. |

## Features

- Framework-neutral core with Avalonia, WPF, .NET MAUI, WinUI, and Uno adapters.
- Runtime culture switching with UI binding refresh.
- `ResourceManager`, `IStringLocalizer`, and custom translation providers behind one provider pipeline.
- Loss-aware import/export for XLIFF, PO/POT, RESX, Android, Apple, ARB, i18next JSON, and CSV/TSV workflows, with source-generated ProTranslate catalogs preferred for runtime.
- XAML-first APIs through `T`/`Translate`, `F`/`Format` markup extensions, attached key/culture/flow-direction properties, and framework-appropriate XML namespace mappings.
- Compiled bindings and WinUI/Uno `x:Bind` sample paths through generated constants, generated translation accessors, generated `ProTranslateStrings` CLR properties, and strongly typed view-model proxies.
- `RegionProfile`-aware culture metadata, explicit region and measurement-system overrides, built-in unit conversion, localized unit formatting, and metric, US customary, imperial, and custom measurement-system mapping.
- Structured runtime diagnostics for missing translations, provider failures, and format failures, with configurable failure policies.
- Roslyn analyzers for missing static keys, placeholder-count mismatches, resource coverage, unsafe dynamic keys, and invalid catalogs.
- Cache policy APIs for lookup caching, missing-key caching, culture-change invalidation, maximum cache entries, and explicit invalidation.
- Thread culture opt-out through `CultureServiceOptions`.
- Automatic and explicit `FlowDirection` handling for left-to-right and right-to-left cultures.
- MVVM-friendly services that do not require a specific view model base class or MVVM framework.
- SOLID package boundaries: providers resolve, the core formats, and adapters adapt.

## Architecture

| Layer | Responsibility |
| --- | --- |
| `ProTranslate.Core` | Culture services, provider abstraction, lookup/fallback, formatting, translation lookup caching, region profiles, measurement-system mapping, unit conversion, diagnostics, and flow direction. |
| `ProTranslate.MicrosoftExtensions` | DI and `IStringLocalizer` integration. |
| `ProTranslate.SourceGenerator` | Strongly typed key constants, accessors, generated bindable strings, generated JSON provider code, and provider manifests for compiled binding, `x:Bind`, and packaging scenarios. |
| `ProTranslate.Analyzers` | Build-time diagnostics over static ProTranslate key usage and catalog files. |
| Framework adapters | XAML markup extensions, attached properties, native property mapping, and binding refresh. |
| `ProTranslate.Formats` and Uno Translation Studio | Import/export, diagnostics, and translation-review workflows over normalized ProTranslate catalogs. |

The adapters are intentionally thin. They map framework-specific concepts such as dependency properties, bindable properties, `FlowDirection`, compiled bindings, and `x:Bind` into core requests, then refresh binding sources when the culture changes.

Compiled-binding-first applications should add `ProTranslate.SourceGenerator` as an analyzer package, include catalogs as `AdditionalFiles`, expose generated `ProTranslateStrings` or equivalent view-model properties, and reserve dynamic adapter markup extensions for concise view-only text or migration cases. The samples follow this pattern: Avalonia enables compiled bindings with `x:DataType`, WinUI and Uno use `x:Bind`, and the shared sample host uses the generated JSON provider instead of assembly resource discovery.

## Usage

### 1. Register ProTranslate services

```csharp
using Microsoft.Extensions.DependencyInjection;
using ProTranslate;

ServiceCollection services = new();

services
    .AddProTranslate(
        culture: CultureInfo.GetCultureInfo("en-US"),
        cultureOptions: new CultureServiceOptions
        {
            ApplyToCurrentThread = false,
            ApplyToDefaultThread = false
        },
        cacheOptions: new TranslationCacheOptions
        {
            MaximumEntries = 2048
        });
```

Applications that do not use dependency injection can construct the core culture manager, provider pipeline, and formatter services directly.

```csharp
var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
var provider = new InMemoryTranslationProvider()
    .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.FileMenu", "File")
    .Add(CultureInfo.GetCultureInfo("pl-PL"), "Shell.FileMenu", "Plik");
var translations = new TranslationService(provider, cultures);

ProTranslate.Avalonia.TranslationService.UseService(translations, cultures);
```

### 2. Use XAML translation markup

Avalonia and WPF can use ProTranslate from the framework default XML namespace after the adapter assembly is referenced:

```xml
<Window
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <StackPanel Translation.Culture="{Binding CurrentCulture}"
              Translation.AutoFlowDirection="True">
    <TextBlock Text="{Translate Shell.FileMenu}" />
  </StackPanel>
</Window>
```

The shared ProTranslate URI remains the portable explicit form for adapters that support `XmlnsDefinition`:

```xml
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:pt="https://github.com/protranslate/xaml">
  <VerticalStackLayout pt:Translation.Culture="{Binding CurrentCulture}"
                       pt:Translation.AutoFlowDirection="True">
    <Label Text="{pt:Translate Shell.FileMenu}" />
  </VerticalStackLayout>
</ContentPage>
```

WinUI does not expose a supported assembly-level `XmlnsDefinitionAttribute`; use `xmlns:pt="using:ProTranslate.WinUI"` there. Uno follows WinUI syntax in the sample, with a ProTranslate URI mapping also emitted for Uno tooling that honors `XmlnsDefinition`.

### 3. Switch culture from a command or service

```csharp
public sealed class SettingsViewModel
{
    private readonly ICultureService _cultures;

    public SettingsViewModel(ICultureService cultures)
    {
        _cultures = cultures;
    }

    public void UsePolish()
    {
        _cultures.SetCulture("pl-PL");
    }
}
```

When the active culture changes, ProTranslate refreshes translated and formatted adapter values plus automatic `FlowDirection` targets. Region and measurement data are exposed through `IGlobalizationService` so view models can refresh those displays through normal MVVM notifications.

## Provider Model

ProTranslate composes translation providers by priority:

- `ResourceManager` for `.resx` and satellite assembly workflows.
- `IStringLocalizer` for ASP.NET Core and Microsoft.Extensions localization workflows.
- Custom providers for JSON, database, remote, or application-owned catalogs.

Provider results include the requested key, resolved value, culture, provider name, and `ResourceNotFound` state. Missing keys return the key by default and are not production crashes.

## Globalization Model

Culture handling is service-based:

- `CultureInfo` drives formatting defaults.
- `UICultureInfo` drives translation lookup.
- `RegionProfile` drives currency, region display, and measurement defaults.
- `IGlobalizationService` exposes explicit region and measurement-system overrides through `SetRegionOverride`, `ClearRegionOverride`, `SetMeasurementSystemOverride`, and `ClearMeasurementSystemOverride`.
- `IUnitConversionService` and `ILocalizedUnitFormatter` provide framework-neutral unit conversion and culture-aware unit text.
- Sample view models demonstrate user-selected region and measurement behavior using the shared core conversion services.
- `FlowDirection` resolves from culture unless an element or view explicitly overrides it.

## Spec-Driven Development

The repository starts with specifications before production code:

- [Architecture Specification](docs/spec/architecture.md)
- [Implementation Plan](docs/spec/implementation-plan.md)
- [API Surface Specification](docs/spec/api-surface.md)
- [Validation Matrix](docs/spec/validation-matrix.md)
- [Translation Formats And Uno App Specification](docs/spec/translation-formats-and-uno-app.md)
- [Release Checklist](docs/spec/release-checklist.md)
- [Package Release Notes](docs/spec/package-release-notes.md)
- [Migration Guide](docs/spec/migration-guide.md)

Each feature should define inputs, outputs, constraints, edge cases, and validation before implementation.

## Sample Validation

Local macOS validation covers the portable solution, core tests, Avalonia adapter tests, docs build, Avalonia sample build, and MAUI Mac Catalyst sample build when MAUI workloads are installed.

Windows CI validates WPF, WinUI, and Uno sample builds. WinUI and Uno sample projects include non-Windows stubs so the repository can restore and inspect on macOS/Linux, but their real XAML app paths are Windows CI validated.

See [samples/README.md](samples/README.md) and [docs/spec/validation-matrix.md](docs/spec/validation-matrix.md) for the current validation split.

## Building Documentation

The documentation site is built with Lunet:

```bash
./build-docs.sh
```

Serve the docs locally:

```bash
./serve-docs.sh
```

## Project Structure

```text
docs/spec/
site/
src/
tests/
samples/
```

## License

MIT. See [LICENSE](LICENSE).
