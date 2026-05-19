# AGENTS.md

## Mission

Implement and evolve ProTranslate as a production-grade XAML translation and globalization framework for:

- Avalonia
- WPF
- .NET MAUI
- WinUI
- Uno Platform

The framework must keep localization, formatting, culture switching, region policy, measurement systems, and flow direction consistent across XAML platforms while preserving a framework-neutral core.

## Ownership Guardrails

Keep changes scoped to the requested package, adapter, sample, workflow, or documentation area. Other contributors may be working in parallel, so do not revert unrelated edits or reshape project boundaries without an explicit task requirement.

## Non-Negotiable Requirements

1. `ProTranslate.Core` remains framework-neutral.
2. Avalonia, WPF, MAUI, WinUI, and Uno adapters remain thin and independent.
3. Runtime culture switching must update translated text, formatted values, `RegionInfo`-derived displays, measurement-system displays, and `FlowDirection`.
4. `ResourceManager`, `IStringLocalizer`, and custom provider pipelines are first-class.
5. XAML usage must support markup extensions and attached properties.
6. Compiled bindings and WinUI/Uno `x:Bind` compatibility must be preserved through source-generated or handwritten CLR members. Reflection-based resource discovery, stringly typed runtime binding paths, and dynamic markup-extension lookup are fallback paths that require an explicit reason.
7. MVVM and SOLID boundaries must stay explicit.
8. Missing keys, fallback paths, invalid formats, and provider failures must produce structured diagnostics.

## Architecture

### Core

`ProTranslate.Core` owns:

- active culture and UI culture snapshots
- provider abstraction and fallback order
- translation lookup
- localized formatting
- `RegionInfo` and region profile services
- measurement system resolution
- framework-neutral flow-direction resolution
- cache policy
- diagnostics

The core must not reference Avalonia, WPF, MAUI, WinUI, Uno, or any adapter package.

### Optional Infrastructure

`ProTranslate.Extensions` owns:

- Microsoft.Extensions.DependencyInjection registration
- options binding
- logging bridge
- `IStringLocalizer` integration

`ProTranslate.SourceGenerator` owns:

- strongly typed key accessors
- strongly typed bindable CLR string surfaces for compiled binding and `x:Bind`
- generated provider code for JSON catalogs when runtime resource discovery is not required
- provider manifests
- compiled binding and `x:Bind`-safe generated members

`ProTranslate.Analyzers` owns:

- missing-key diagnostics
- placeholder mismatch diagnostics
- resource coverage diagnostics
- unsafe dynamic key warnings

### Adapters

Adapters own only framework integration:

- markup extensions
- attached properties
- dependency or bindable properties
- dispatcher marshalling
- native `FlowDirection` mapping
- weak target tracking
- design-time fallback behavior

Adapters must not duplicate provider fallback, formatting, region, measurement, or diagnostics policy from core.

### Compiled Binding Defaults

New samples and docs must prefer the generated `ProTranslate.Generated.ProTranslateStrings` class, generated key constants, generated accessor methods, or equivalent handwritten view-model properties. Avalonia samples must keep compiled bindings enabled and set `x:DataType`; WinUI and Uno samples must keep normal text and formatted text on `x:Bind`-safe view-model properties. WPF and MAUI should use typed view-model properties where their XAML compilers cannot statically validate a markup-extension path.

Adapter markup extensions remain supported for concise view-only XAML and migration scenarios, but they must not be presented as the primary path for trimming-sensitive, NativeAOT-sensitive, or compiled-binding-first applications. Do not introduce reflection-based catalog loading in samples or runtime code unless the spec calls out why generated provider code or explicit provider registration is insufficient.

## Spec-Driven Workflow

Before implementing a feature, update or create a spec that includes:

- Inputs: culture, keys, bindings, provider data, options, user actions.
- Outputs: API behavior, UI refresh, diagnostics, generated files.
- Constraints: package boundaries, threading, performance, trimming, framework compatibility.
- Edge cases: missing keys, invalid culture, provider failures, disposed targets, neutral cultures, RTL content.
- Validate: unit tests, adapter tests, analyzer tests, sample builds, docs build.

Use the files under `docs/spec/` as the source of truth:

- `architecture.md`
- `implementation-plan.md`
- `api-surface.md`
- `validation-matrix.md`

## XAML API Direction

Representative XAML should remain concise:

```xml
<TextBlock Text="{pt:Translate Shell.FileMenu}" />
<TextBlock Text="{pt:Format Orders.Total, Value={Binding Total}}" />
<Grid pt:Globalization.FlowDirection="Auto" />
<TextBlock pt:Translate.Key="Orders.EmptyState" />
```

Framework-specific syntax may differ, but the concepts must remain aligned across Avalonia, WPF, MAUI, WinUI, and Uno.

## Culture And Region Policy

Culture state is snapshot-based. Avoid hidden global state.

Required behavior:

- explicit culture and UI culture
- explicit fallback chain
- optional region override
- optional measurement-system override
- deterministic provider fallback
- dispatcher-safe target refresh

Do not mutate `Thread.CurrentThread.CurrentCulture` globally unless the host explicitly opts in.

## Quality Bar

Feature work is complete only when:

1. The relevant spec includes inputs, outputs, constraints, edge cases, and validation.
2. Core behavior has unit tests.
3. Adapter behavior has framework-appropriate validation.
4. Culture switching is tested.
5. Memory retention risk is checked for adapter target tracking.
6. Public documentation is updated for user-facing behavior.
7. Lunet docs build when docs are changed.

## Documentation

The docs site uses Lunet and follows the sibling repository pattern used by MediaPlayer and Lottie.

Build docs:

```bash
./build-docs.sh
```

Serve docs locally:

```bash
./serve-docs.sh
```
