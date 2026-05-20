---
title: Package Release Notes
description: Package contents, package-by-package notes, known limitations, and upgrade guidance.
---

# Package Release Notes

These notes describe the current package set. They are written for package consumers and release reviewers, so they separate shipped behavior from planned work.

## Release Summary

The current release ships:

- Framework-neutral abstractions and core services.
- Runtime culture switching.
- Parent, fallback, and default culture lookup.
- `string.Format` based localized formatting.
- Translation lookup cache policy and explicit invalidation.
- Thread culture opt-out options.
- Observable localized strings for binding-friendly view-models.
- Structured diagnostics for missing translations, provider failures, and format failures.
- Region profile metadata.
- Measurement-system metadata and overrides.
- Built-in unit conversion and localized unit formatting.
- Flow-direction resolution.
- `ResourceManager` and `IStringLocalizer` provider bridges.
- Avalonia, WPF, MAUI, WinUI, and Uno adapters.
- Source generator for key constants, translation accessors, and provider manifests.
- Analyzer package for static key, placeholder, and catalog coverage diagnostics.
- Optional format tooling for industry translation exchange files.
- Avalonia runtime UI automation and release-only adapter leak tests.
- Samples for each supported UI framework.
- Lunet documentation site.
- CI, docs, pack, and release workflows.

## Package Notes

### `ProTranslate.Abstractions`

Contains shared contracts and value types:

- `ICultureService`.
- `ITranslationProvider`.
- `ITranslationService`.
- `IGlobalizationService`.
- `IObservableLocalizedString`.
- `LocalizedString`.
- `CultureChangedEventArgs`.
- Diagnostic types.
- Region and measurement types.
- `TextFlowDirection`.

Use this package in shared libraries that need to depend on contracts without taking a dependency on the default runtime implementation.

### `ProTranslate.Core`

Contains default implementations:

- `CultureService`.
- `TranslationService`.
- `GlobalizationService`.
- `InMemoryTranslationProvider`.
- `CompositeTranslationProvider`.
- Region profile provider.
- Measurement-system resolver.
- Unit conversion service.
- Localized unit formatter.
- Translation cache policy and invalidation.

Use this package in application composition roots and tests.

### `ProTranslate.ResourceManager`

Provides `ResourceManagerTranslationProvider` for `.resx` and satellite assembly workflows.

Use this package when an application already uses .NET resource files or wants standard .NET resource fallback.

### `ProTranslate.MicrosoftExtensions`

Provides optional Microsoft.Extensions integration:

- `AddProTranslate`.
- `IStringLocalizer` provider integration.

Use this package when the application has a Microsoft.Extensions.DependencyInjection composition root.

### Framework Adapter Packages

Adapter packages provide XAML integration:

- `ProTranslate.Avalonia`.
- `ProTranslate.Wpf`.
- `ProTranslate.Maui`.
- `ProTranslate.WinUI`.
- `ProTranslate.Uno`.

Each adapter includes markup extensions, attached properties, a binding source, a static adapter bridge, and a framework-specific DI helper.

### `ProTranslate.SourceGenerator`

Generates:

- `ProTranslateKeys`.
- `ProTranslateAccessors`.
- `ProTranslateStrings`.
- `ProTranslateGeneratedTranslationProvider`.
- `ProTranslateProviderManifest`.

Supported inputs:

- `*.protranslate.keys.txt`.
- `Strings.*.json`.
- `*.protranslate.json`.

Generator diagnostics:

- `PTSG001`: invalid JSON.
- `PTSG002`: duplicate key in one file.

### `ProTranslate.Analyzers`

Reports:

- `PTA001`: missing static key.
- `PTA002`: placeholder count mismatch.
- `PTA003`: resource coverage gap.
- `PTA004`: unsafe dynamic key.
- `PTA005`: invalid catalog.

### `ProTranslate.Formats`

Provides optional import/export infrastructure for normalized ProTranslate catalogs and industry exchange formats. This is tooling and authoring infrastructure, not hidden runtime provider behavior:

- ProTranslate JSON.
- i18next JSON.
- Flutter ARB.
- RESX.
- Android `strings.xml`.
- Apple `.strings`, `.stringsdict`, and `.xcstrings`.
- gettext PO/POT.
- XLIFF 1.2 and XLIFF 2.x workflows.
- CSV and TSV.

Use this package for tooling and authoring workflows. Runtime application lookup should prefer source-generated ProTranslate catalogs or explicitly registered providers.

### Uno Translation Studio Sample

The `samples/ProTranslate.Uno.TranslationStudio` project is a professional authoring sample over the format tooling surface. It demonstrates a compact Uno review UI, source/target editing, review-state actions, format import/export review summaries, generated `ProTranslateStrings` for `x:Bind`, and normalized ProTranslate catalogs as the runtime output path.

Release notes should keep this separate from `ProTranslate.Uno`, which is the runtime XAML adapter. Full authoring workflow support still requires documented Windows build validation and UI smoke coverage.

## XAML Namespace Notes

Avalonia and WPF support prefix-free sample syntax through default namespace mappings. MAUI supports the shared ProTranslate URI but not third-party additions to the protected MAUI default namespace. WinUI uses `using:ProTranslate.WinUI`. Uno uses WinUI-compatible syntax in samples and has a shared URI mapping for Uno tooling that honors `XmlnsDefinition`.

See [XAML Namespaces](xaml-namespaces.md).

## Known Limitations

Remaining hardening work:

- Analyzer code fixes.
- Logging bridge or debug overlay.
- Rich provider trace and cache hit/miss diagnostics.
- Broad WPF, MAUI, WinUI, and Uno runtime UI automation.
- Full fidelity and round-trip validation for every industry format construct.
- Full Uno Translation Studio authoring workflow validation.
- Deeper dispatcher and retained-target stress tests beyond current Avalonia coverage.

`CultureService` construction captures the initial culture without mutating ambient thread state. Explicit `SetCulture` calls apply selected cultures to current and default thread cultures by default. Use `CultureServiceOptions` to opt out in tests and hosts that need strict culture isolation.

Package notes should list only the formats that are implemented and validated. XLIFF should be positioned as the preferred CAT/TMS exchange format, and source-generated ProTranslate catalogs should remain the preferred runtime format for applications. Lossy or unsupported conversions must be surfaced through diagnostics.

## Upgrade Guidance

For applications moving between ProTranslate releases:

- Keep package versions aligned across core, adapters, source generator, and analyzers.
- Rebuild generated source after changing catalog files.
- Prefer generated keys in view models and strongly typed bindings.
- Re-run the relevant sample build for each target framework.
- Re-read [Migration Guide](migration-guide.md) when XAML namespace or adapter behavior changes.

The deeper package notes remain in [docs/spec/package-release-notes.md](https://github.com/wieslawsoltes/ProTranslate/blob/main/docs/spec/package-release-notes.md).
