# ProTranslate Architecture Specification

## Purpose

ProTranslate is a professional XAML translation and globalization framework for .NET UI applications. It provides a framework-neutral core and thin adapters for Avalonia, WPF, .NET MAUI, WinUI, and Uno so application teams can localize text, formatting, layout direction, region data, and culture-specific UI behavior without coupling view models to one XAML framework.

The design follows spec-driven development: every feature starts with explicit inputs, outputs, constraints, edge cases, and validation before implementation work begins.

## Implementation Status

The current repository contains the first stable implementation of the core product architecture. Remaining work is hardening and platform-depth rather than missing core feature pillars.

Implemented:
- framework-neutral abstractions and core services for culture events, translation lookup, fallback, cache policy, formatting, observable localized strings, structured diagnostics, `RegionProfile`, measurement-system mapping, unit conversion, localized unit formatting, region and measurement overrides, thread-culture opt-out, and text-flow direction
- `ResourceManager`, `IStringLocalizer`, in-memory, and composite provider paths
- Avalonia, WPF, MAUI, WinUI, and Uno adapters with `TranslateExtension`/`TExtension`, `FormatExtension`/`FExtension`, binding-source refresh, attached key/fallback/string-format properties, `Translation.Culture`, and `Translation.AutoFlowDirection`
- XML namespace mappings through `XmlnsDefinition`/`XmlnsPrefix` where the target XAML stack supports them
- source generation of key constants, get/value/format/observe accessors, bindable strings, generated JSON provider code, and provider manifests from text and JSON catalogs
- analyzer diagnostics for static key usage, placeholder counts, catalog coverage, dynamic keys, and invalid catalogs
- core tests, source-generator tests, analyzer tests, Avalonia adapter smoke/runtime/leak tests, cross-platform CI for portable projects, macOS sample validation for Avalonia/MAUI paths, and Windows CI validation for WPF/WinUI/Uno samples

Remaining hardening:
- broader loss-aware round-trip validation for XLIFF, gettext PO/POT, RESX, Android, Apple, Flutter ARB, i18next JSON, and CSV/TSV exchange
- professional Uno Translation Studio validation for catalog authoring, diagnostics review, and source-generator-ready output
- analyzer code fixes
- richer provider traces, cache diagnostics, and logging/debug-overlay integrations
- broader runtime UI automation for WPF, MAUI, WinUI, and Uno
- deeper weak target tracking and leak-test infrastructure outside current Avalonia coverage

## Product Goals

- Keep localization logic out of views and view models while remaining MVVM-friendly.
- Support runtime culture switching without application restart.
- Provide XAML-first APIs through markup extensions and attached properties.
- Support compiled bindings and `x:Bind`-style generated binding paths without dynamic reflection requirements in normal usage.
- Work with `ResourceManager`, `IStringLocalizer`, and custom translation providers.
- Interoperate with industry translation formats through optional, loss-aware tooling while keeping source-generated ProTranslate catalogs as the preferred application runtime path.
- Centralize culture, region, measurement system, calendar, number, currency, date, time, pluralization, and `FlowDirection` decisions.
- Keep adapters thin and replaceable so the core remains framework-neutral and testable.

## Non-Goals

- ProTranslate does not own visual theming, styling, or control templates.
- ProTranslate does not replace .NET resource generation, satellite assemblies, or localization authoring tools.
- ProTranslate runtime services do not parse every CAT/TMS or platform exchange format. Import/export belongs in optional tooling or authoring applications.
- ProTranslate does not prescribe one MVVM framework.
- ProTranslate can avoid mutating `Thread.CurrentThread.CurrentCulture` and default thread cultures when the host configures `CultureServiceOptions` for isolation.
- ProTranslate does not require UI frameworks to share binary dependencies with each other.

## Package Model

| Package | Responsibility |
| --- | --- |
| `ProTranslate.Abstractions` | Framework-neutral service contracts, result types, diagnostics, culture events, region/measurement models, and text-flow enum. |
| `ProTranslate.Core` | Framework-neutral culture service, translation service, provider composition, in-memory provider, cache policy, formatting, region profiles, unit conversion, measurement-system mapping, and flow-direction resolution. |
| `ProTranslate.ResourceManager` | `ResourceManager` provider for `.resx` and satellite assembly localization. |
| `ProTranslate.MicrosoftExtensions` | Optional Microsoft.Extensions dependency injection and `IStringLocalizer` provider registration. |
| `ProTranslate.Avalonia` | Avalonia `T`/`Translate` and `F`/`Format` markup extensions, attached key/culture/flow-direction properties, binding-source refresh, and DI helper. |
| `ProTranslate.Wpf` | WPF `T`/`Translate` and `F`/`Format` markup extensions, dependency properties, binding-source refresh, `XmlLanguage`, and `FlowDirection` integration. |
| `ProTranslate.Maui` | MAUI `T`/`Translate` and `F`/`Format` markup extensions, bindable attached properties, binding-source refresh, and flow-direction integration. |
| `ProTranslate.WinUI` | WinUI `T`/`Translate` and `F`/`Format` markup extensions, dependency properties, binding-source refresh, and flow-direction integration. |
| `ProTranslate.Uno` | Uno adapter aligned with WinUI markup-extension and attached-property conventions. |
| `ProTranslate.SourceGenerator` | Generated strongly typed key constants, translation accessors, bindable string properties, JSON provider code, and provider manifests from text and JSON additional files. |
| `ProTranslate.Analyzers` | Build-time diagnostics for static keys, placeholders, catalog coverage, dynamic keys, and invalid catalogs. |
| `ProTranslate.Formats` | Optional loss-aware import/export for XLIFF 1.2/2.1 workflows, gettext PO/POT, RESX, Android `strings.xml`, Apple `.strings`/`.stringsdict`/`.xcstrings`, Flutter ARB, i18next JSON, and CSV/TSV exchange. |
| Uno Translation Studio | Professional authoring and review app over normalized ProTranslate catalogs and shared format tooling. |

## Core Architecture

```text
Application
  |
  | culture selection, DI, user preferences
  v
ProTranslate.Core
  |-- Culture state and notifications
  |-- Translation provider pipeline
  |-- Formatting, cache policy, and region profile metadata
  |-- Measurement system mapping
  |-- Unit conversion and localized unit formatting
  |-- Flow direction resolver
  |-- Cache and diagnostics
  |
  +--> ResourceManager provider
  +--> IStringLocalizer provider
  +--> JSON/custom providers
  +--> Generated strongly typed accessors

Optional Tooling
  |-- XLIFF 1.2/2.1 import/export
  |-- PO/POT, RESX, Android, Apple, ARB, i18next, CSV/TSV import/export
  |-- Loss-aware diagnostics
  +-- Source-generator-ready ProTranslate catalogs

Framework Adapters
  |-- Avalonia
  |-- WPF
  |-- MAUI
  |-- WinUI
  +-- Uno
```

The core owns the domain model and all deterministic decisions. Adapters translate framework concepts into core requests and subscribe to culture-change notifications to invalidate bindings, markup extensions, and attached property targets.

Format import/export is intentionally outside the runtime lookup path. XLIFF is the preferred CAT/TMS exchange format, and generated ProTranslate catalogs are the preferred runtime catalog format for compiled-binding-first applications.

## Core Services

### Culture State

`ICultureService` owns the active `CultureInfo`, UI culture, inferred region, and notifications.

Inputs:
- requested UI culture
- requested formatting culture
- optional user region override
- optional fallback chain

Outputs:
- current culture and UI culture properties
- `CultureChanged` notification with old and new snapshots
- region, metric, and flow-direction metadata

Constraints:
- culture updates are atomic
- notification ordering is deterministic
- adapters refresh through framework binding paths; broader dispatcher abstractions remain planned
- current/default thread culture mutation is configurable through `CultureServiceOptions`

Edge cases:
- invalid culture name
- neutral culture without region
- missing satellite assembly
- culture switch during concurrent lookups
- UI target disposed while update is pending

Validation:
- switching from `en-US` to `pl-PL` updates text, dates, numbers, currency, and direction-dependent properties in one cycle
- planned concurrent lookup stress tests do not observe partially updated snapshots
- planned disposed adapter target retention tests

`IGlobalizationService` also raises `CultureChanged` for region and measurement override changes. These events carry the current cultures as both old and new values because the culture did not change, but the globalization metadata visible to view models did.

### Translation Provider Pipeline

`ITranslationProvider` resolves keys into localized values. Providers are composed by priority.

Inputs:
- resource key
- culture snapshot
- optional namespace or resource scope
- optional arguments
- optional fallback behavior

Outputs:
- found translation value
- missing-key result with diagnostics metadata
- provider and format failure diagnostics

Constraints:
- provider lookup must not depend on a UI framework
- fallback order is explicit and testable
- missing keys are observable without forcing exceptions in production
- lookup cache policy is explicit through `TranslationCacheOptions`

Edge cases:
- key exists in parent culture but not specific culture
- provider throws
- format string placeholders do not match arguments
- provider returns null or empty string intentionally
- multiple providers contain same key

Validation:
- `ResourceManager` and `IStringLocalizer` providers produce equivalent fallback behavior for common .NET resource cases
- provider priority tests prove deterministic first-match behavior
- current missing-key metadata includes key, culture, provider, resource-not-found state, and structured diagnostics; richer scope and fallback-path traces remain planned

### Formatting And Region Metadata

`ITranslationService.Format` formats translated values using the active formatting culture and `string.Format`.

Inputs:
- value
- format string or named format profile
- culture snapshot
- optional region override

Outputs:
- localized string
- normal .NET formatting result or structured format-failure diagnostic with configured return/throw behavior

Constraints:
- use .NET globalization primitives where possible
- preserve caller-owned values; `CultureService` can avoid current/default thread culture mutation, while `StringLocalizerTranslationProvider` temporarily uses thread culture mechanisms required by `IStringLocalizer`
- custom formatter services beyond `string.Format` and the built-in unit formatter remain extension points

Edge cases:
- region differs from language (`en-PL`, `fr-CA`, `es-US`)
- right-to-left culture with left-to-right embedded value
- unknown currency for custom region
- measurement conversion unavailable
- calendar differs from Gregorian

Validation:
- current tests cover selected culture, flow-direction, and measurement-system behavior
- broader date, number, currency, percent, list, enum, and region override tests remain planned

### Measurement System Mapping

The current implementation exposes measurement-system mapping through `IGlobalizationService.MeasurementSystem` and `MeasurementSystemProfile`, plus built-in conversion and localized unit formatting services.

Inputs:
- culture snapshot
- optional `RegionInfo`
- optional user preference

Outputs:
- `MeasurementSystem.USCustomary` for US, LR, and MM regions
- `MeasurementSystem.Imperial` for GB
- `MeasurementSystem.Metric` when `RegionInfo.IsMetric` is true
- `MeasurementSystem.Custom` for remaining cases
- default length, temperature, mass, volume, and speed unit labels on `MeasurementSystemProfile`
- converted measurement values through `IUnitConversionService`
- localized measurement text through `ILocalizedUnitFormatter`

Constraints:
- core user preference overrides region defaults
- dedicated data-driven unit profiles remain replaceable extension points
- adapters only display the result; they do not infer region policy

Edge cases:
- neutral culture without region
- unsupported or synthetic region
- mixed unit preferences
- culture switch after unit-bound text is already rendered

Validation:
- `en-US` defaults to US customary, `en-GB` to imperial-compatible preferences where configured, and `pl-PL` to metric
- sample user override survives culture switch through sample view-model state

### Flow Direction Resolver

The core flow-direction resolver maps UI culture metadata to left-to-right or right-to-left layout.

Inputs:
- culture snapshot
- optional explicit override
- optional content direction metadata

Outputs:
- framework-neutral `TextFlowDirection`

Constraints:
- explicit view-level overrides win
- framework adapters map to native `FlowDirection` types
- culture switch updates attached-property targets

Edge cases:
- bidirectional text inside an LTR shell
- numeric and Latin text inside RTL culture
- view has explicit direction different from application direction
- framework does not support per-element flow direction consistently

Validation:
- Arabic and Hebrew cultures resolve to RTL by default
- explicit element override is preserved during culture switch
- adapter tests verify correct native property mapping

## Adapter Responsibilities

Adapters are small integration layers. They must not duplicate provider, formatting, region, measurement, fallback, or diagnostics logic.

Each adapter provides:
- markup extension for translated strings through `T` and `Translate`
- markup extension for formatted strings through `F` and `Format`
- XML namespace mappings where supported by the framework
- attached key, fallback, and string-format properties
- attached properties for culture and automatic flow direction
- binding-source invalidation bridge for culture switching
- native flow-direction mapping
- DI helper that connects application services to the adapter binding source

Planned adapter work:
- explicit dispatcher abstractions
- weak target tracking and memory-retention tests
- design-time safe behavior where the framework supports it

Adapter constraints:
- no adapter references another UI framework adapter
- no view model base class requirement
- no dependency on a specific MVVM framework
- compiled binding and `x:Bind` compatibility must be preserved through strongly typed APIs, generated constants, generated accessors, and generated string properties

## Markup Extension Model

Common XAML intent:

```xml
<TextBlock Text="{Translate Shell.FileMenu}" />
<Grid Translation.Culture="{Binding CurrentCulture}"
      Translation.AutoFlowDirection="True" />
```

The prefix-free form is validated for Avalonia and WPF because those adapters can map into the framework default XML namespace. MAUI and Uno expose the shared `https://github.com/protranslate/xaml` URI where their XAML tooling supports `XmlnsDefinition`; WinUI uses `using:ProTranslate.WinUI`.

Inputs:
- key
- scope
- arguments or binding values
- fallback text
- culture override

Outputs:
- localized text
- refreshed target value after culture switch
- diagnostics for missing key or invalid format

Constraints:
- static keys support generated strongly typed accessors
- dynamic keys are allowed but analyzer-visible as lower confidence
- updates must not require replacing the whole view

Edge cases:
- key changes after binding update
- argument changes without culture change
- culture changes while argument binding is unset
- target property is not a string
- markup extension used at design time

Validation:
- culture switching updates markup extension values in Avalonia, WPF, MAUI, WinUI, and Uno samples
- generated key and string-property APIs compile with compiled bindings and `x:Bind`
- dynamic key scenarios are covered by runtime tests and analyzer warnings

## MVVM And SOLID Requirements

MVVM:
- view models depend on `ICultureService`, `ITranslationService`, typed key services, or formatter abstractions only when they own text-producing behavior
- view-only text stays in XAML through markup extensions or attached properties
- commands can switch culture through injected services

SOLID:
- single responsibility: providers resolve, formatters format, adapters adapt, analyzers analyze
- open/closed: new providers and formatters are added through interfaces
- Liskov substitution: providers obey common result semantics
- interface segregation: consumers can depend on translation, formatting, culture, or diagnostics independently
- dependency inversion: adapters and applications depend on abstractions

## Diagnostics

Diagnostics include:
- missing key
- provider name
- invalid format string
- provider exception

Planned diagnostics include:
- missing culture
- fallback used
- unsupported measurement system
- invalid culture switch
- retained target warning in test builds

Diagnostics outputs:
- current implementation: `LocalizedString.ResourceNotFound`, `LocalizedString.ProviderName`, `LocalizedString.Diagnostics`, `ITranslationService.DiagnosticReported`, optional diagnostic sinks, and analyzer reports for build-time checks
- planned: richer provider traces, optional logging integration, and optional debug overlay hooks

## Security And Reliability

- Translation values are treated as data, not executable markup.
- Providers that load external files validate paths and encodings.
- Format strings are validated against arguments before display when possible.
- Culture switching must be cancellation-aware where async providers are used.
- Weak references are used for UI targets tracked by adapters.

## Acceptance Criteria

- Core package builds without referencing Avalonia, WPF, MAUI, WinUI, or Uno.
- Each adapter references only the core and its target framework.
- Runtime culture switching updates translated strings, formatted values, `RegionProfile`-driven sample displays, sample measurement-system displays, and adapter `FlowDirection`.
- ResourceManager and `IStringLocalizer` providers are both first-class.
- Format import/export tooling, when implemented, is loss-aware, keeps XLIFF as the preferred CAT/TMS exchange format, and emits source-generator-ready ProTranslate catalogs for runtime usage.
- `T`/`Translate` and `F`/`Format` markup extensions plus key/culture/flow-direction attached properties are available for XAML-first usage.
- Compiled binding and `x:Bind` compatibility is validated with generated constants, generated accessors, generated string properties, or strongly typed sample proxy patterns.
- Analyzer code fixes, richer provider/cache diagnostics, logging/debug overlays, broader non-Avalonia runtime UI automation, and deeper retained-target stress tests remain tracked as hardening work.
