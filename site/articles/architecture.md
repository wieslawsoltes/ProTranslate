---
title: Architecture
description: ProTranslate package boundaries, runtime model, adapter responsibilities, and extension points.
---

# Architecture

ProTranslate is a framework-neutral translation and globalization core with thin XAML adapters for Avalonia, WPF, .NET MAUI, WinUI, and Uno Platform. The core owns culture state, translation lookup, fallback, cache policy, formatting, diagnostics, region metadata, unit conversion, localized unit formatting, measurement-system mapping, and text-flow decisions. Adapter packages only translate those decisions into native XAML markup extensions, attached properties, dependency or bindable properties, binding refresh, and native `FlowDirection` values.

The architecture follows spec-driven development. New behavior should define inputs, outputs, constraints, edge cases, and validation before code is changed.

## Design Goals

- Keep translation policy out of UI framework adapters.
- Keep view models independent from Avalonia, WPF, MAUI, WinUI, and Uno packages.
- Support runtime culture switching without restarting the application.
- Support XAML-first usage through `Translate`/`T`, `Format`/`F`, and `Translation.*` attached properties.
- Support compiled binding and `x:Bind` scenarios through generated constants, generated accessors, generated string properties, and strongly typed view-model proxy members.
- Support professional localization workflows through optional loss-aware import/export tooling while keeping source-generated ProTranslate catalogs as the preferred runtime path.
- Provide structured diagnostics for missing keys, provider failures, and format failures.
- Keep remaining extension points visible without documenting them as shipped behavior.

## Current Implementation Status

Implemented:

- Core abstractions in the `ProTranslate` namespace.
- `CultureService`, `TranslationService`, `GlobalizationService`, observable localized strings, fallback behavior, and diagnostics.
- `InMemoryTranslationProvider`, `CompositeTranslationProvider`, `ResourceManagerTranslationProvider`, and `StringLocalizerTranslationProvider`.
- Region profile, measurement-system metadata, built-in unit conversion, localized unit formatting, and explicit region/measurement overrides.
- Text-flow resolution from .NET culture metadata.
- Avalonia, WPF, MAUI, WinUI, and Uno adapters.
- XML namespace mappings where the target XAML framework supports them.
- Source generation from `*.protranslate.keys.txt`, `Strings.*.json`, and `*.protranslate.json`, including key accessors and provider manifests.
- Analyzer diagnostics for static key usage, placeholders, catalog coverage, dynamic keys, and invalid catalogs.
- Cache policy APIs and thread-culture opt-out options.
- Core tests, source-generator tests, analyzer tests, Avalonia adapter smoke/runtime/leak tests, docs build, package build, and CI workflows.

Remaining extension points:

- Analyzer code fixes.
- Cache hit/miss diagnostics and richer provider traces.
- Logging bridges and debug overlays over diagnostics.
- Broader runtime UI automation for WPF, MAUI, WinUI, and Uno.
- Broader round-trip validation for XLIFF, PO/POT, RESX, Android, Apple, ARB, i18next JSON, and CSV/TSV.
- Professional Uno Translation Studio validation for catalog authoring and review.

## Package Boundaries

| Package | Responsibility |
| --- | --- |
| `ProTranslate.Abstractions` | Service contracts, culture events, localized result types, diagnostics, region and measurement models, and framework-neutral flow direction. |
| `ProTranslate.Core` | Culture service, translation service, provider composition, cache policy, formatting, fallback, diagnostics, region profiles, unit conversion, measurement-system resolution, and flow-direction resolution. |
| `ProTranslate.ResourceManager` | Provider bridge for `.resx`, `ResourceManager`, and satellite assembly workflows. |
| `ProTranslate.MicrosoftExtensions` | Optional Microsoft.Extensions dependency injection and `IStringLocalizer` integration. |
| `ProTranslate.SourceGenerator` | Strongly typed keys, translation accessors, generated bindable strings, generated JSON provider code, and provider manifests generated from additional catalog files. |
| `ProTranslate.Analyzers` | Build-time validation for static keys, placeholder counts, catalog coverage, dynamic keys, and invalid catalogs. |
| `ProTranslate.Avalonia` | Avalonia markup extensions, attached properties, binding source, namespace mapping, and DI helper. |
| `ProTranslate.Wpf` | WPF markup extensions, dependency properties, binding source, `XmlLanguage`, namespace mapping, and DI helper. |
| `ProTranslate.Maui` | MAUI markup extensions, bindable attached properties, binding source, namespace mapping, and DI helper. |
| `ProTranslate.WinUI` | WinUI markup extensions, dependency properties, binding source, `x:Bind` sample support, and DI helper. |
| `ProTranslate.Uno` | Uno adapter aligned with WinUI patterns and Uno namespace tooling. |
| `ProTranslate.Formats` | Import/export, normalization, and diagnostics for industry translation formats. |
| Uno Translation Studio | Professional authoring and review UI over normalized ProTranslate catalogs. |

Adapters must not duplicate provider lookup, fallback, formatting, region, unit conversion, measurement, or diagnostic policy. If multiple adapters need the same behavior, it belongs in the core.

Format tooling must also stay outside adapter packages. XLIFF is the preferred CAT/TMS exchange format, and source-generated ProTranslate catalogs are the preferred runtime format.

## Runtime Flow

```text
Application settings or command
  |
  v
ICultureService
  |
  +-- CultureChanged event
  +-- CurrentCulture for formatting
  +-- CurrentUICulture for lookup
  +-- FlowDirection
  |
  v
ITranslationService
  |
  +-- ITranslationProvider pipeline
  +-- fallback cultures
  +-- string.Format
  +-- diagnostics
  |
  v
Framework adapter
  |
  +-- markup extensions
  +-- attached properties
  +-- binding source refresh
  +-- native FlowDirection
```

The application chooses a culture through `ICultureService` or `IGlobalizationService`. The translation service raises the same culture-change event and refreshes observable localized strings. Adapters subscribe through a binding source or attached-property update path and map the new values into native UI framework properties.

Region and measurement overrides are metadata changes rather than culture changes, but `IGlobalizationService` still raises `CultureChanged` when those overrides change so application view models have one refresh signal for translated text and globalization-derived displays.

## Culture State

`ICultureService` owns:

- `CurrentCulture`, used for number, date, currency, and `string.Format`.
- `CurrentUICulture`, used for translation lookup.
- `CurrentRegion`, inferred from the current culture when possible.
- `IsMetric`, derived from .NET region metadata.
- `FlowDirection`, derived from `CultureInfo.TextInfo.IsRightToLeft`.
- `CultureChanged`, raised after a successful culture switch.

`CultureService` construction captures the initial culture without mutating ambient process or thread culture. By default, explicit `SetCulture` calls apply cultures to `Thread.CurrentThread`, `CultureInfo.DefaultThreadCurrentCulture`, and `CultureInfo.DefaultThreadCurrentUICulture`. Hosts can set `CultureServiceOptions.ApplyToCurrentThread` and `ApplyToDefaultThread` to `false` when they need culture isolation.

## Translation Pipeline

`ITranslationService` resolves a key by asking the configured provider pipeline for the requested UI culture, optional parent cultures, configured fallback cultures, and optional default culture. Missing keys return the key by default and report a warning diagnostic.

Provider failures are reported as `ProviderFailure` diagnostics. Depending on `TranslationFallbackOptions.ProviderFailureBehavior`, the service either continues to the next fallback candidate or rethrows after reporting.

Formatting is explicit. `ITranslationService.Format` first resolves the localized string, then calls `string.Format(CurrentCulture, localized.Value, arguments)`. Invalid format strings report `FormatFailure` diagnostics and return the unformatted localized value by default.

`TranslationCacheOptions` controls lookup caching, missing-key caching, culture-change invalidation, and maximum cache entries. `ITranslationCacheInvalidator` exposes explicit invalidation for hosts that reload provider data.

## Globalization Model

`IGlobalizationService` combines cultures, translations, region profile, measurement profile, and flow direction behind one app-facing service.

Implemented region and measurement behavior:

- Region profile is derived from the active culture unless the app sets a region override.
- Measurement system can be overridden independently from language and region.
- US, Liberia, and Myanmar map to US customary.
- Great Britain maps to imperial.
- Regions reported by .NET as metric map to metric.
- Other regions map to custom.

`IUnitConversionService` and `ILocalizedUnitFormatter` provide framework-neutral unit conversion and culture-aware unit text. `IGlobalizationService` exposes `ConvertMeasurement`, `FormatMeasurement`, and `FormatMeasurementForProfile` helpers for view-model code.

## Adapter Responsibilities

Each adapter provides the same concepts with native framework types:

- `TranslateExtension` and `TExtension`.
- `FormatExtension` and `FExtension`.
- `TranslationBindingSource`.
- Static adapter `TranslationService` bridge.
- Attached `Translation.Key`.
- Attached `Translation.FallbackValue`.
- Attached `Translation.StringFormat`.
- Attached `Translation.Culture`.
- Attached `Translation.AutoFlowDirection`.
- `AddProTranslate{Framework}` dependency injection helper.

Avalonia and WPF also map ProTranslate types into the default framework XML namespace, so samples can use prefix-free syntax where the application references the adapter assembly. MAUI exposes the shared ProTranslate URI but does not allow third-party additions to the protected MAUI default namespace. WinUI uses `using:ProTranslate.WinUI`. Uno uses WinUI-compatible syntax in samples and emits a shared URI mapping for Uno tooling that honors `XmlnsDefinition`.

## Source Generation

`ProTranslate.SourceGenerator` reads catalog files supplied as `AdditionalFiles` and emits `ProTranslate.Generated.ProTranslateKeys`, `ProTranslateAccessors`, `ProTranslateStrings`, `ProTranslateGeneratedTranslationProvider`, and `ProTranslateProviderManifest`.

Generated APIs are framework-neutral and support compiled binding-friendly code:

```csharp
translations.Value_ShellTitle();
translations.Format_OrdersTotal(total);
translations.Observe_ShellTitle();
var strings = new ProTranslateStrings(translations);
```

`ProTranslateStrings` is the default CLR surface for Avalonia compiled bindings and WinUI/Uno `x:Bind`. `ProTranslateGeneratedTranslationProvider` is available when JSON catalog values should be compiled into provider code instead of discovered through runtime resources.

The generator validates invalid JSON catalogs and duplicate keys within a file. `ProTranslate.Analyzers` performs consuming-code validation for missing keys, placeholder count mismatches, resource coverage, unsafe dynamic keys, and invalid catalogs.

## Translation Format Tooling

Import/export tooling handles XLIFF 1.2/2.1 workflows, gettext PO/POT, RESX, Android `strings.xml`, Apple `.strings`/`.stringsdict`/`.xcstrings`, Flutter ARB, i18next JSON, and CSV/TSV. The tooling normalizes external files into ProTranslate catalogs, reports loss-aware diagnostics for unsupported constructs, and emits source-generator-ready runtime catalogs.

XLIFF is the preferred CAT/TMS exchange format. Source-generated ProTranslate catalogs are the preferred runtime format.

## Extensibility Points

Stable extension points:

- `ITranslationProvider` for resource stores.
- `IRegionProfileProvider` for region policy.
- `IMeasurementSystemResolver` for measurement policy.
- `IProTranslateDiagnosticSink` for host diagnostics.
- View-model translation proxy members for compiled binding and `x:Bind`.

Extension packages should depend on abstractions and core services first. Framework-specific packages should only be introduced when the extension has native XAML integration.

## Validation Expectations

Architecture changes should update:

- Core unit tests for service behavior.
- Source-generator tests for generated API changes.
- Adapter tests or sample builds for XAML behavior.
- Documentation articles for user-facing behavior.
- CI workflows when validation scope changes.

When docs change, run:

```bash
./build-docs.sh
```

The deeper working specification remains in [docs/spec/architecture.md](https://github.com/wieslawsoltes/ProTranslate/blob/main/docs/spec/architecture.md).
