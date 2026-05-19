---
title: Validation Matrix
description: Required validation coverage for core services, adapters, source generation, samples, docs, and releases.
---

# Validation Matrix

ProTranslate spans portable services, Roslyn source generation, five XAML frameworks, docs, packaging, and release automation. Validation is split so portable behavior runs everywhere and platform-specific sample builds run where the toolchain exists.

## Local Fast Path

Run this before handing off normal source changes:

```bash
./build.sh
./build-docs.sh
git diff --check
```

Run package validation when public API, metadata, or project files change:

```bash
./pack.sh 0.1.0-validation.local
```

## Coverage By Area

| Area | Validation | Current status |
| --- | --- | --- |
| Core services | `tests/ProTranslate.Tests` | Implemented. |
| Source generator | `tests/ProTranslate.Tests` generator tests | Implemented. |
| Analyzer package | `tests/ProTranslate.Analyzers.Tests` | Implemented. |
| Avalonia adapter | `tests/ProTranslate.Avalonia.Tests` and sample build | Implemented smoke, runtime, and leak coverage. |
| WPF adapter | Windows sample build | Implemented CI build coverage. |
| MAUI adapter | macOS adapter build and sample restore with MAUI workload | Implemented build coverage; full app packaging is local/platform validation. |
| WinUI adapter | Windows sample build | Implemented CI build coverage. |
| Uno adapter | Windows sample build | Implemented CI build coverage. |
| Docs site | `./build-docs.sh` | Implemented. |
| Packages | `./pack.sh <version>` | Implemented. |
| Release | `.github/workflows/release.yml` | Implemented. |
| Translation format tooling | Parser/writer, round-trip, loss diagnostics, placeholder/plural mapping, and source-generator compile tests | Initial surface exists; validation hardening planned. |
| Uno Translation Studio | Build and UI smoke tests for import preview, editing, diagnostics, and export preview | Initial sample exists; validation hardening planned. |

## Core Runtime Scenarios

Validate:

- `SetCulture` changes `CurrentCulture` and `CurrentUICulture`.
- Same-culture updates are idempotent.
- `CultureChanged` includes old and new culture snapshots.
- Parent-culture fallback works.
- Explicit fallback cultures work.
- Default-culture fallback works.
- Missing keys report `MissingTranslation`.
- Provider exceptions report `ProviderFailure`.
- Format exceptions report `FormatFailure`.
- Format failures return the unformatted localized value by default.
- Report-and-throw options rethrow after diagnostics.
- Observable localized strings refresh on culture changes.
- Translation cache options and explicit invalidation work.
- Thread-culture opt-out leaves current/default thread cultures unchanged.
- Unit conversion and localized unit formatting produce expected values.

## Provider Scenarios

Validate:

- In-memory provider returns exact culture matches.
- Empty string values are treated as intentional values.
- Missing values are represented by `ResourceNotFound`.
- Composite provider uses deterministic order.
- Provider diagnostics are preserved.
- ResourceManager provider follows .NET resource fallback.
- IStringLocalizer provider respects the active UI culture.

Implemented provider-adjacent validation:

- Cache policy behavior.
- Provider manifests.

Planned provider validation:

- Remote provider latency and failure handling.
- Provider trace events.

## Globalization Scenarios

Validate:

- `en-US` maps to US customary measurement defaults.
- `en-GB` maps to imperial measurement defaults.
- `pl-PL` maps to metric measurement defaults.
- Region override changes region profile without changing language.
- Measurement-system override wins over region defaults.
- Clearing overrides returns to culture-derived behavior.
- Region and measurement override changes raise `IGlobalizationService.CultureChanged`.
- Arabic and Hebrew cultures resolve right-to-left flow direction.
- Explicit UI framework flow direction overrides are preserved where the adapter supports them.

Implemented globalization validation:

- Full unit conversion.
- Localized unit formatting.
- Thread-culture opt-out behavior.

Planned globalization validation:

- Persisted user preference policy.

## Source Generator Scenarios

Validate:

- `*.protranslate.keys.txt` produces constants.
- `Strings.*.json` produces constants.
- `*.protranslate.json` produces constants.
- Nested JSON keys flatten into stable dotted keys.
- Invalid JSON reports `PTSG001`.
- Duplicate keys in one file report `PTSG002`.
- Identifier collisions receive stable hash suffixes.
- Generated accessors compile against `ITranslationService`.
- Provider manifest entries include keys, cultures, source files, and placeholder indexes.
- Importer-produced normalized catalogs compile through the source generator before any import/export support is claimed.

## Translation Format Scenarios

Before claiming format support, validate:

- XLIFF 1.2 and 2.1 import/export fixtures.
- gettext PO/POT import/export fixtures.
- RESX string import/export fixtures and non-string diagnostics.
- Android `strings.xml` strings, arrays, plurals, and styled-string diagnostics.
- Apple `.strings`, `.stringsdict`, and `.xcstrings` import/export fixtures.
- Flutter ARB values, descriptions, placeholders, plurals, and selects.
- i18next JSON nested keys, namespaces, interpolation, and plural suffix conventions.
- CSV/TSV required columns, encoding, duplicate keys, and formula-looking cells.
- Loss-aware diagnostics for unsupported constructs.
- Placeholder mapping across .NET composite formatting, ICU, printf, XLIFF inline codes, Android XML, and i18next interpolation.
- Plural mapping across gettext, Android, Apple, ARB, and i18next.

XLIFF is the preferred CAT/TMS exchange format. Source-generated ProTranslate catalogs are the preferred runtime format.

## Uno Translation Studio Scenarios

Before claiming app support, validate:

- Uno app build for each documented target head.
- Import preview.
- Dense translation-grid editing.
- Culture, state, key, and diagnostics filters.
- Placeholder validation before save and export.
- Export preview with loss diagnostics.
- Source-generator compile check after saving catalogs.

## Analyzer Validation

- Missing key references.
- Placeholder count mismatch.
- Resource coverage by culture.
- Unsafe dynamic keys.
- Invalid catalogs.

## Adapter Scenarios

For every adapter, validate:

- `Translate` resolves static XAML keys.
- `T` resolves static XAML keys.
- `Format` resolves formatted values where the framework supports the binding pattern.
- `F` resolves formatted values where the framework supports the binding pattern.
- `Translation.Key` updates the target property.
- `Translation.FallbackValue` is used for missing keys where implemented.
- `Translation.StringFormat` applies a framework-local string format.
- `Translation.Culture` drives a culture change.
- `Translation.AutoFlowDirection` maps to native flow direction.
- Binding source refreshes after culture changes.

Framework-specific validation:

- Avalonia: compiled binding sample path, default namespace mapping, runtime culture/format refresh, attached-property refresh, automatic flow direction, and release-only leak tests.
- WPF: default presentation namespace mapping and `XmlLanguage`.
- MAUI: shared ProTranslate URI and bindable property behavior.
- WinUI: `using:ProTranslate.WinUI` and `x:Bind` sample path.
- Uno: WinUI-compatible syntax and Uno sample build.

## Documentation Validation

Docs changes must validate:

- `site/articles/menu.yml` references existing files.
- Every page has front matter.
- Relative article links point to existing pages.
- `./build-docs.sh` succeeds.
- New public behavior appears in the correct guide and, when architectural, in the spec articles.

## Release Validation

Before release:

- CI is green on Linux, macOS, and Windows.
- Docs workflow builds.
- Sample builds pass on the appropriate OS.
- Package artifacts include expected `.nupkg` and `.snupkg` files.
- Package metadata is correct.
- Release notes list only shipped behavior.
- Known preview limitations are explicit.

The deeper working matrix remains in [docs/spec/validation-matrix.md](https://github.com/wieslawsoltes/ProTranslate/blob/main/docs/spec/validation-matrix.md).
