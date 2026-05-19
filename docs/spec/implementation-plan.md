# ProTranslate Implementation Plan

## Development Model

ProTranslate uses spec-driven development. Every feature issue should include:

- Inputs: data, configuration, state, and user actions the feature receives.
- Outputs: public API behavior, UI effect, diagnostics, and generated artifacts.
- Constraints: compatibility, threading, performance, framework, and package boundaries.
- Edge cases: invalid, missing, concurrent, design-time, and platform-specific cases.
- Validate: unit, integration, analyzer, sample, and documentation checks.

This keeps implementation work auditable before code is written and gives reviewers objective acceptance criteria.

## Current Phase Status

| Phase | Status | Notes |
| --- | --- | --- |
| 0 Repository Foundation | Done | Solution, package metadata, docs site, CI, and ownership guidance exist. |
| 1 Core Domain | Preview done | Culture service, translation service, fallback options, observable strings, structured diagnostics, cache policy, thread-culture opt-out, flow direction, region profiles, and measurement mapping exist. Logging/debug integrations remain planned. |
| 2 Provider Implementations | Preview done | In-memory, composite, `ResourceManager`, and `IStringLocalizer` providers exist. Provider/format failure diagnostics, throw/continue policies, cache policy options, and explicit cache invalidation exist. Richer provider traces remain planned. |
| 3 Formatting, Region, And Units | Preview done | Formatting uses `string.Format`; reusable region profile, measurement resolver, unit conversion, and localized unit formatter services exist with region and measurement overrides. Custom formatter extension points remain planned. |
| 4 Source Generation And Analyzers | Preview done | Source generator emits key constants, get/value/format/observe accessors, bindable strings, generated JSON provider code, and provider manifests from text and JSON catalogs. Analyzer package reports missing keys, placeholder mismatches, coverage gaps, dynamic keys, and invalid catalogs. |
| 5 Avalonia Adapter | Preview done | `T`/`Translate`, `F`/`Format`, attached key/fallback/string-format/culture/flow-direction properties, binding-source refresh, sample, smoke/runtime tests, and release-only leak tests exist. |
| 6 WPF Adapter | Preview done | Adapter, default XML namespace mapping, and sample exist; Windows CI validates the sample build. UI automation and leak tests remain planned. |
| 7 MAUI Adapter | Preview done | Adapter, shared ProTranslate XAML URI, and sample exist; macOS CI/local validation covers the Mac Catalyst path when workloads are installed. Broader platform lifecycle tests remain planned. |
| 8 WinUI And Uno Adapters | Preview done | Adapters and samples exist; Uno emits a shared URI mapping, WinUI uses `using:` because assembly `XmlnsDefinition` is unsupported, and Windows CI validates real XAML sample builds. Platform-specific runtime testing remains planned. |
| 9 Documentation, Samples, And Release | Preview done | README/spec/site/sample validation docs, release checklist, package release notes, migration guide, docs workflow, analyzer test CI, sample CI, and pack workflow are present. |
| 10 Translation Format Tooling And Uno Authoring | Initial implementation | `ProTranslate.Formats` and Uno Translation Studio surfaces exist for loss-aware import/export and authoring. Broader round-trip, diagnostics, and UI validation remain hardening work. |

## Phase 0: Repository Foundation

Inputs:
- empty repository
- MIT license
- desired framework-neutral architecture

Outputs:
- solution and project skeleton
- package metadata conventions
- documentation and specification baseline

Constraints:
- do not couple core to a UI framework
- keep package IDs stable from the start
- document public direction before implementation

Edge cases:
- package names conflict with existing packages
- future adapter cannot share abstractions cleanly
- docs imply API that cannot be implemented

Validate:
- docs build
- package map reviewed against architecture spec
- ownership boundaries documented in `AGENTS.md`

## Phase 1: Core Domain

Inputs:
- requested culture
- requested UI culture
- optional region override
- translation keys
- formatting requests

Outputs:
- `ICultureService`
- current culture and UI culture state
- translation provider interfaces
- formatting service interfaces
- region and measurement system models
- diagnostics contracts

Constraints:
- no XAML framework references
- thread-safe reads during culture switch
- deterministic fallback chain
- nullable annotations enabled

Edge cases:
- invalid culture name
- neutral cultures
- provider exceptions
- concurrent culture switching
- missing resource values

Validate:
- unit tests for culture state, fallback, provider priority, missing-key state, and formatting
- concurrency tests for culture switch and lookup
- public API review for adapter sufficiency

## Phase 2: Provider Implementations

Inputs:
- `.resx` and satellite resources
- `ResourceManager`
- `IStringLocalizer`
- optional custom providers

Outputs:
- `ResourceManagerTranslationProvider`
- `StringLocalizerTranslationProvider`
- provider composition and priority rules
- missing-key metadata through `LocalizedString`
- provider diagnostics

Constraints:
- keep provider results culture-aware
- explicit translation cache policy and invalidation APIs
- provider exception policy options

Edge cases:
- missing satellite assembly
- fallback to parent culture
- empty string as intentional value
- argument count mismatch
- duplicate keys across providers

Validate:
- provider equivalence tests for common fallback paths
- missing-key tests with `LocalizedString.ResourceNotFound`
- provider failure and format failure diagnostics tests

## Phase 3: Formatting, Region, And Units

Inputs:
- culture snapshot
- `RegionInfo`
- user region preference
- measurement preference
- value and format profile

Outputs:
- current `string.Format`-based localized formatting
- `RegionProfile`, `IsMetric`, `MeasurementSystem`, and `MeasurementSystemProfile` metadata
- reusable region override and measurement-system override APIs
- built-in unit conversion and localized unit formatting services
- planned custom formatter extension points

Constraints:
- current/default thread culture mutation is configurable through `CultureServiceOptions`

Edge cases:
- neutral culture without region
- language and region mismatch
- unknown currency
- explicit custom region profile
- non-Gregorian calendar

Validate:
- implemented culture matrix tests for selected cultures in core tests
- implemented measurement defaults for `en-US`, `en-GB`, and `pl-PL`
- implemented region and measurement override tests
- planned broader snapshot tests for stable formatted output

## Phase 4: Source Generation And Analyzers

Inputs:
- resource manifests
- `.resx` files
- optional JSON or PO catalogs
- project MSBuild properties

Outputs:
- strongly typed key accessors
- provider manifests
- generated binding-safe observable members
- analyzer diagnostics for missing keys, placeholder mismatch, coverage gaps, dynamic keys, and invalid catalogs

Constraints:
- incremental generator
- deterministic output
- no runtime reflection requirement for generated key, string, and provider paths
- compatible with trimming and NativeAOT where possible

Edge cases:
- duplicate generated member names
- invalid resource keys
- culture file added or removed
- generated constants, accessors, and `ProTranslateStrings` properties used by compiled bindings or `x:Bind`

Validate:
- generator snapshot tests
- generated accessor compile tests
- text and JSON catalog tests
- generator diagnostic tests
- analyzer diagnostics tests and future code-fix tests where applicable
- compiled sample using generated keys

## Phase 5: Avalonia Adapter

Inputs:
- Avalonia binding targets
- markup extension keys
- attached property values
- active culture snapshot

Outputs:
- `TranslateExtension`
- `FormatExtension`
- shared ProTranslate XML namespace mapping
- optional default XML namespace mapping for prefix-free sample usage
- attached key/fallback/string-format properties
- attached `Culture` and `AutoFlowDirection` properties
- binding-source invalidation service
- `FlowDirection` mapping

Constraints:
- no WPF, MAUI, WinUI, or Uno references
- compatible with compiled bindings
- planned weak target tracking
- planned design-time fallback support

Edge cases:
- target disposed during culture switch
- key binding changes independently of culture
- attached property inherited through visual/logical tree
- design preview without application services

Validate:
- headless Avalonia tests for markup extension refresh
- release-only leak tests for detached targets
- compiled binding sample
- `FlowDirection` switch sample

## Phase 6: WPF Adapter

Inputs:
- WPF dependency objects
- binding expressions
- markup extension keys
- culture snapshot

Outputs:
- WPF `T`/`Translate` markup extensions
- shared and default XML namespace mappings
- `Culture` and `AutoFlowDirection` dependency properties
- `XmlLanguage` integration
- `FlowDirection` mapping

Constraints:
- stay WPF-only
- use WPF binding refresh paths
- broader dispatcher and memory-retention guarantees remain planned

Edge cases:
- resource lookup during design mode
- target property is not string
- `Language` differs from UI culture
- binding refresh conflicts with validation errors

Validate:
- Windows CI sample build
- planned WPF UI tests or integration harness
- culture switch sample
- planned memory retention tests

## Phase 7: MAUI Adapter

Inputs:
- MAUI bindable objects
- XAML markup extension keys
- active culture snapshot

Outputs:
- MAUI `T`/`Translate` markup extensions
- shared ProTranslate XML namespace mapping
- `Culture` and `AutoFlowDirection` bindable attached properties
- binding-source refresh bridge
- flow direction mapping

Constraints:
- broader platform-specific dispatcher behavior remains planned
- no dependency on Avalonia, WPF, WinUI, or Uno
- mobile lifecycle reactivation tests remain planned

Edge cases:
- page unloaded while update is pending
- culture switch during navigation
- platform-specific flow direction behavior
- single-project asset packaging differences

Validate:
- MAUI Mac Catalyst sample build on macOS when workloads are installed
- planned lifecycle refresh tests where practical
- package validation

## Phase 8: WinUI And Uno Adapters

Inputs:
- WinUI or Uno dependency objects
- `x:Bind`-friendly strongly typed sample properties
- markup extension keys
- active culture snapshot

Outputs:
- WinUI adapter
- Uno adapter
- Uno shared ProTranslate XML namespace mapping where supported by Uno tooling
- `FlowDirection` mapping
- strongly typed sample properties for `x:Bind`

Constraints:
- keep WinUI and Uno assemblies separate
- avoid APIs unavailable on Uno target platforms
- preserve strongly typed binding compatibility

Edge cases:
- WebAssembly globalization data availability
- platform-specific culture APIs
- `x:Bind` generated code lifecycle in sample apps
- target unloaded during update

Validate:
- WinUI sample build on Windows CI
- Uno sample build on Windows CI
- planned Uno Skia or WebAssembly runtime validation
- `x:Bind` sample compile check on Windows CI

## Phase 9: Documentation, Samples, And Release

Inputs:
- implemented APIs
- validated adapter samples
- package artifacts

Outputs:
- docs site
- README
- sample apps
- package release notes
- migration guide
- release checklist
- CI and release workflows

Constraints:
- README must not overpromise unsupported adapters
- docs examples must compile
- package versions stay aligned
- docs must state which sample paths are locally validated and which are Windows CI validated

Edge cases:
- adapter released after core
- partial provider support
- sample target framework drift
- MAUI workload unavailable on a local machine
- Windows-only samples inspected on non-Windows machines

Validate:
- docs build
- sample build matrix
- package validation
- release checklist

## Phase 10: Translation Format Tooling And Uno Authoring

Status: initial implementation present; validation hardening required.

Inputs:
- XLIFF 1.2 and 2.1 files from CAT/TMS workflows
- gettext PO and POT files
- `.resx` files from .NET resource workflows
- Android `strings.xml`
- Apple `.strings`, `.stringsdict`, and `.xcstrings`
- Flutter ARB files
- i18next JSON resources
- CSV and TSV exchange files
- normalized ProTranslate catalogs and source-generator manifests
- translator edits, review state, conflict resolution, and export commands

Outputs:
- loss-aware import diagnostics
- normalized ProTranslate catalog records
- source-generator-ready JSON catalogs
- key-only catalog files where no localized value exists
- export files in the selected industry format
- professional Uno Translation Studio app for import preview, translation editing, diagnostics review, and export preview

Constraints:
- XLIFF is the preferred CAT/TMS exchange format
- source-generated ProTranslate catalogs are the preferred application runtime format
- import/export belongs to optional tooling and authoring surfaces, not `ProTranslate.Core`
- adapters must not parse exchange formats
- unsupported metadata must produce structured diagnostics instead of being silently dropped
- generator support should consume normalized ProTranslate catalogs unless a direct format has a specific deterministic build-time use case

Edge cases:
- format-specific plural rules and placeholder syntaxes
- duplicate keys and conflicting external IDs
- neutral cultures, pseudo-locales, and ecosystem-specific locale names
- comments, context, review state, and metadata that cannot round-trip
- non-string RESX resources, styled Android strings, Apple variation metadata, ICU ARB messages, and spreadsheet formula-looking cells
- right-to-left target cultures in authoring UI

Validate:
- parser and writer unit tests for every claimed format
- round-trip tests with explicit loss diagnostics
- placeholder and plural mapping tests
- culture tag normalization tests
- importer-produced catalog compile tests through `ProTranslate.SourceGenerator`
- analyzer tests over imported catalogs
- Uno Translation Studio build and UI smoke tests for documented target heads
- docs build

## Definition Of Done

A feature is done when:

- Spec is updated with inputs, outputs, constraints, edge cases, and validation.
- Public API is covered by tests or documented manual validation.
- Adapter behavior is validated in the target framework.
- Culture switch behavior is tested.
- Missing-key and failure diagnostics are observable.
- README or docs are updated when user-facing behavior changes.
- Sample validation scope is documented honestly for local, macOS CI, and Windows CI paths.
