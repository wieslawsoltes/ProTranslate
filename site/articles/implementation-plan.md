---
title: Implementation Plan
description: Spec-driven development phases, completion status, and remaining work for ProTranslate.
---

# Implementation Plan

ProTranslate uses spec-driven development for agentic coding. Work starts with behavior, constraints, edge cases, and validation, then moves into implementation. This keeps a broad multi-framework surface from becoming a collection of adapter-specific decisions.

## Working Method

Every feature should define:

- Inputs: keys, cultures, regions, values, provider data, bindings, user actions, and host options.
- Outputs: service results, UI updates, generated files, diagnostics, and package artifacts.
- Constraints: package boundaries, threading, trimming, framework compatibility, and performance.
- Edge cases: missing keys, neutral cultures, provider exceptions, invalid formats, disposed UI targets, and RTL text.
- Validation: unit tests, generator tests, adapter tests, sample builds, docs build, package validation, and CI coverage.

## Phase 1: Repository And Package Foundation

Status: implemented.

Deliverables:

- Solution structure.
- Central package management.
- Shared build properties.
- Package metadata.
- `ProTranslate.Abstractions`.
- `ProTranslate.Core`.
- Adapter package projects.
- Samples and tests.
- Lunet docs project.

Validation:

```bash
dotnet restore ProTranslate.slnx
dotnet build ProTranslate.slnx -c Release
```

## Phase 2: Core Translation Runtime

Status: implemented.

Deliverables:

- `ICultureService`.
- `ITranslationProvider`.
- `ITranslationService`.
- `TranslationCacheOptions`.
- `ITranslationCacheInvalidator`.
- `CultureServiceOptions`.
- `LocalizedString`.
- Fallback options.
- Provider and format failure policies.
- Cache policy and explicit invalidation.
- Thread culture opt-out.
- Missing-key diagnostics.
- Provider-failure diagnostics.
- Format-failure diagnostics.
- Observable localized strings.

Validation:

```bash
dotnet test tests/ProTranslate.Tests/ProTranslate.Tests.csproj -c Release
```

## Phase 3: Provider Integrations

Status: implemented for preview providers.

Deliverables:

- In-memory provider for tests, samples, and embedded catalogs.
- Composite provider for ordered lookup.
- ResourceManager provider for `.resx` and satellite assemblies.
- IStringLocalizer provider for Microsoft.Extensions localization workflows.

Remaining hardening:

- Rich provider traces.
- Remote provider reference package, if needed by future scenarios.

## Phase 4: Globalization Runtime

Status: implemented for culture, region, measurement metadata, unit conversion, localized unit formatting, and flow direction.

Deliverables:

- `IGlobalizationService`.
- `RegionProfile`.
- `MeasurementSystemProfile`.
- Region override.
- Measurement-system override.
- Default measurement-system resolver.
- Built-in unit conversion service.
- Localized unit formatter.
- Framework-neutral `TextFlowDirection`.

Remaining hardening:

- Persistent user preference policy.

## Phase 5: XAML Adapters

Status: implemented across Avalonia, WPF, MAUI, WinUI, and Uno.

Deliverables:

- `Translate` and `T` markup extensions.
- `Format` and `F` markup extensions.
- Translation binding source.
- Attached `Translation.Key`.
- Attached `Translation.FallbackValue`.
- Attached `Translation.StringFormat`.
- Attached `Translation.Culture`.
- Attached `Translation.AutoFlowDirection`.
- DI registration helpers.
- XML namespace mappings where supported.

- Avalonia adapter smoke, runtime, and release-only leak tests.
- Framework sample builds.
- Manual runtime culture-switch verification where native UI tooling is available.

Remaining hardening:

- Broader runtime UI automation for WPF, MAUI, WinUI, and Uno.
- Dispatcher abstraction hardening.

## Phase 6: Source Generation

Status: implemented for key generation, accessor generation, provider manifests, and analyzer diagnostics.

Deliverables:

- Incremental source generator.
- Text key catalog support.
- JSON catalog support.
- `ProTranslateKeys`.
- `ProTranslateAccessors`.
- `ProTranslateProviderManifest`.
- Invalid JSON diagnostic `PTSG001`.
- Duplicate key diagnostic `PTSG002`.
- Placeholder compatibility analysis.
- Missing-key analysis for XAML and generated accessors.
- Resource coverage diagnostics.
- Unsafe dynamic key diagnostics.
- Invalid catalog analyzer diagnostics.

Remaining hardening:

- Code fixes.

## Phase 7: Samples

Status: implemented for each supported framework.

Sample expectations:

- MVVM view model.
- Runtime culture switching.
- Region display.
- Measurement-system display.
- Formatting examples.
- Markup extension usage.
- Attached-property usage.
- Compiled binding or `x:Bind` path where supported.
- Prefix-free XAML where supported by the target framework.

Validation split:

- macOS validates portable projects, Avalonia, and MAUI sample paths.
- Windows CI validates WPF, WinUI, and Uno sample paths.

## Phase 8: Documentation

Status: expanded.

Documentation levels:

- Getting started and installation.
- XAML namespace guidance.
- Provider, formatting, globalization, and diagnostics guides.
- Source generator guide.
- MVVM and extensibility guides.
- Framework-specific guides.
- Testing, CI, release, troubleshooting, and FAQ.
- Architecture, API, implementation plan, validation matrix, release checklist, release notes, and migration guide.

Docs validation:

```bash
./build-docs.sh
```

## Phase 8.5: Translation Format Tooling And Uno Authoring

Status: initial implementation present; validation hardening required.

Planned deliverables:

- loss-aware import/export for XLIFF 1.2 and 2.1
- gettext PO/POT import/export
- .NET RESX import/export
- Android `strings.xml` import/export
- Apple `.strings`, `.stringsdict`, and `.xcstrings` import/export
- Flutter ARB import/export
- i18next JSON import/export
- CSV/TSV spreadsheet exchange
- normalized ProTranslate catalog records
- source-generator-ready catalog output
- professional Uno Translation Studio app for import preview, editing, diagnostics, and export preview

Guidance:

- Prefer XLIFF for CAT/TMS exchange.
- Prefer source-generated ProTranslate catalogs for application runtime.
- Treat format import/export as optional tooling, not adapter runtime behavior.
- Require diagnostics for unsupported or lossy constructs.

Required validation before claiming support:

- parser and writer tests for every listed format
- round-trip fixtures with loss diagnostics
- placeholder and plural mapping tests
- source-generator compile tests over importer output
- analyzer tests over imported catalogs
- Uno app build and UI smoke tests for documented target heads

## Phase 9: CI, Packaging, And Release

Status: implemented for preview package production.

Deliverables:

- Cross-platform CI.
- Docs build workflow.
- Sample build workflow.
- Preview pack workflow.
- Release workflow.
- NuGet push behind `NUGET_API_KEY`.
- GitHub release generation for tags.

Validation:

```bash
./build.sh
./pack.sh 0.1.0-preview.local
```

## Remaining Work Backlog

The next professional hardening phases should focus on:

- Analyzer code fixes.
- Rich provider traces and cache hit/miss diagnostics.
- Logging and debug-overlay integrations.
- Runtime UI automation for WPF, MAUI, WinUI, and Uno.
- Translation format tooling round-trip validation and professional Uno Translation Studio UI smoke coverage.
- Deeper dispatcher and retained-target stress tests.
- API compatibility checks before stable releases.
- Signed package and symbol publishing policy.

The deeper working plan remains in [docs/spec/implementation-plan.md](https://github.com/wieslawsoltes/ProTranslate/blob/main/docs/spec/implementation-plan.md).
