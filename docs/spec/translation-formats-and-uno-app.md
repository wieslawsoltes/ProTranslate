# Translation Formats And Uno App Specification

## Status

This specification describes optional import/export tooling, source-generator integration, and a professional Uno translation app. The repository includes initial `ProTranslate.Formats` and Uno Translation Studio surfaces, but release notes must only claim the format fidelity, diagnostics, and app workflows that are implemented and validated. Do not present format conversion as application runtime lookup behavior.

## Purpose

ProTranslate should interoperate with industry-standard localization workflows without making application runtime code parse every exchange format. Translation teams need CAT/TMS exchange formats, platform-native file formats, and spreadsheet handoff formats. ProTranslate applications need deterministic, source-generated catalogs that are safe for compiled bindings, WinUI/Uno `x:Bind`, trimming, NativeAOT, and CI validation.

Preferred flow:

1. Import or export industry formats through `ProTranslate.Formats` or Uno Translation Studio.
2. Normalize content into ProTranslate catalog data with explicit diagnostics.
3. Generate ProTranslate source catalogs and manifests for application runtime.
4. Keep runtime lookup, fallback, formatting, diagnostics, culture switching, and provider priority in `ProTranslate.Core`.

XLIFF is the preferred CAT/TMS exchange format. Source-generated ProTranslate catalogs are the preferred app runtime format.

## Ownership Boundaries

Format import/export belongs to optional tooling and authoring surfaces. It must not add UI-framework dependencies to `ProTranslate.Core`.

Package and tool responsibilities:

- `ProTranslate.Core` keeps translation lookup, fallback, formatting, diagnostics, culture snapshots, region policy, measurement policy, and flow direction.
- `ProTranslate.SourceGenerator` consumes normalized ProTranslate catalog inputs and emits key constants, accessors, bindable CLR string surfaces, generated providers, and provider manifests.
- `ProTranslate.Formats` parses and writes external formats, maps them to normalized catalog records, and reports loss-aware diagnostics.
- The Uno Translation Studio app provides a professional authoring and review workflow over normalized catalogs and import/export services.
- Adapters remain thin and do not parse exchange formats.

## Supported Exchange Formats

| Format | Direction | Rationale | Loss-aware notes |
| --- | --- | --- | --- |
| XLIFF 1.2 | Import/export | Broad CAT/TMS compatibility and legacy enterprise workflows. | Preserve source, target, notes, state, context, and inline placeholders where representable. Report unsupported segmentation, skeleton, phase, and vendor extension data. |
| XLIFF 2.1 | Import/export | Modern CAT/TMS exchange with stronger module model. | Preserve units, segments, notes, state, and inline codes where supported. Report unsupported modules such as advanced metadata, validation, or change tracking when they are not represented by the normalized model. |
| gettext PO/POT | Import/export | Open-source translator workflow and template extraction. | Preserve comments, references, contexts, plural entries, and fuzzy state where possible. Report unsupported plural formulas, obsolete entries, or translator comments that cannot round-trip. |
| .NET RESX | Import/export | Interop with `ResourceManager`, Visual Studio resource editors, and satellite assembly workflows. | Preserve string resources and comments. Report non-string data, binary resources, type converters, and file references that cannot become ProTranslate string records. |
| Android `strings.xml` | Import/export | Android platform localization and MAUI/Android interop. | Preserve string names, string arrays, plurals, and basic XML escaping. Report spans, styled text, quantity rules, and translatable flags that cannot map cleanly. |
| Apple `.strings` | Import/export | iOS, macOS, and native Apple localization interop. | Preserve key/value strings and comments. Report duplicate keys, encoding problems, and escape sequences that require normalization. |
| Apple `.stringsdict` | Import/export | Apple plural and variable dictionary support. | Preserve plural variable names and categories when they map to ProTranslate plural metadata. Report nested rules and format-specifier constructs that need manual review. |
| Apple `.xcstrings` | Import/export | Current Xcode string catalog workflow. | Preserve localizations, comments, states, screenshots or context references where supported by the normalized model. Report unsupported variations, device-specific data, or tool metadata. |
| Flutter ARB | Import/export | Flutter and Intl workflow interop. | Preserve message values, descriptions, placeholder metadata, and plurals/selects where mapped. Report ICU message constructs that ProTranslate cannot validate yet. |
| i18next JSON | Import/export | Web, JavaScript, and cross-platform application interop. | Preserve nested keys, namespaces, and plural suffix conventions where configured. Report interpolation syntax mismatches and arrays or objects that are not string entries. |
| CSV/TSV | Import/export | Spreadsheet, vendor, and product-manager review workflows. | Preserve configured columns for key, source, target cultures, comments, state, and context. Report duplicate keys, missing required columns, formula-looking cells, and delimiter/encoding ambiguity. |

## Normalized Catalog Model

Inputs:

- resource key
- source text
- localized target text by culture
- optional namespace or resource scope
- comments, developer notes, translator notes, and context
- placeholder metadata
- plural and select metadata where supported
- workflow state such as new, translated, reviewed, needs review, fuzzy, or obsolete
- source file, line, and external reference information

Outputs:

- normalized ProTranslate catalog records
- source-generator-compatible JSON catalogs where app runtime needs compiled provider code
- key-only catalogs where only static key API generation is required
- provider manifests with keys, cultures, source files, placeholder indexes, and diagnostics metadata
- export files in the selected external format
- structured diagnostics for unsupported, lossy, invalid, duplicate, or ambiguous constructs

Constraints:

- normalized records must be deterministic and source-control friendly
- key identity must remain stable across import/export cycles
- external state must not silently overwrite reviewed strings
- import/export must be explicit tooling behavior, not hidden runtime provider behavior
- exported files must use predictable encoding, newline, and ordering policies
- source-generated catalogs remain the recommended runtime path for compiled-binding-first apps

Edge cases:

- duplicate keys inside one file or across imported files
- culture tags that differ between ecosystems, such as `en-US`, `en_US`, `en-rUS`, or Apple locale identifiers
- neutral cultures and pseudo-locales
- plural categories that differ between CLDR, gettext, Apple, Android, ARB, and i18next conventions
- placeholder syntax differences, including .NET composite formatting, ICU, printf, named interpolation, XML inline tags, and XLIFF inline codes
- comments and context metadata that cannot be represented by the target format
- binary, styled, attributed, or platform-specific resources
- malformed XML, JSON, PO, RESX, ARB, or delimited input
- spreadsheet formulas, leading `=`, delimiter injection, and encoding ambiguity

## Loss-Aware Diagnostics

Format tooling must never drop unsupported constructs silently. Diagnostics should include:

- diagnostic ID
- severity
- source format
- source file and location where known
- key or unit ID
- culture where relevant
- unsupported construct name
- whether data was preserved, approximated, ignored, or requires manual review
- suggested repair or alternate format when available

Default policy:

- fail import for malformed files that cannot be parsed safely
- continue with warnings for unsupported metadata that does not affect string values
- mark records as needing review when placeholders, plurals, or workflow states are approximate
- fail export when the target format cannot represent required string semantics without explicit loss approval

## Source Generator Integration

Format import/export should produce source-generator-friendly ProTranslate catalogs as its primary runtime output. The source generator should not become a general-purpose parser for every exchange format unless a specific format has a stable, deterministic, build-time use case.

Inputs:

- normalized ProTranslate JSON catalogs
- key-only catalog files
- optional generated provider manifests from import tooling
- MSBuild `AdditionalFiles`

Outputs:

- `ProTranslateKeys`
- `ProTranslateAccessors`
- `ProTranslateStrings`
- `ProTranslateGeneratedTranslationProvider`
- `ProTranslateProviderManifest`
- diagnostics for invalid normalized catalogs, duplicate keys, placeholder mismatches, and coverage gaps

Constraints:

- generator output remains framework-neutral
- generated CLR properties remain usable from Avalonia compiled bindings and WinUI/Uno `x:Bind`
- runtime app startup must not depend on reflection-based catalog discovery for the normal path
- format-specific diagnostics that require full exchange-file semantics belong in the importer, not the generator

## Professional Uno Translation App

The Uno Translation Studio app is an authoring and review surface, not an adapter feature. It should run on Uno-supported desktop targets first, then expand to other heads after validation.

Inputs:

- ProTranslate normalized catalogs
- imported XLIFF, PO/POT, RESX, Android, Apple, ARB, i18next JSON, CSV, or TSV files
- project manifests or source-generator manifests
- user-selected source culture, target cultures, and fallback culture
- translator edits, review state changes, search/filter actions, and export commands

Outputs:

- edited normalized catalogs
- export files in supported formats
- generated source-catalog files suitable for `ProTranslate.SourceGenerator`
- diagnostics report for lossy import/export decisions
- unresolved issue list for missing keys, placeholder mismatches, coverage gaps, and unsupported constructs

Professional workflow requirements:

- dense, keyboard-friendly translation grid
- side-by-side source and target editing
- culture and file filters
- missing, stale, fuzzy, reviewed, and needs-review states
- placeholder validation before save/export
- plural and context inspection
- import review with conflict resolution
- export review with loss diagnostics
- integration guidance for Uno `x:Bind` and generated `ProTranslateStrings`

Constraints:

- use the same framework-neutral import/export services as command-line tooling
- keep Uno UI code out of core packages
- persist edits in deterministic files suitable for review
- do not mutate application runtime culture state as part of authoring
- keep app sample claims limited to validated heads and workflows

Edge cases:

- large catalogs with thousands of keys
- partially translated cultures
- simultaneous edits and file reloads
- target file format cannot represent plural or placeholder metadata
- imported files use different key naming conventions
- right-to-left target cultures
- translator edits invalid placeholders

## Validation

Required validation before claiming implementation:

- parser and writer unit tests for each supported format
- round-trip tests with loss diagnostics for each format
- culture tag normalization tests
- placeholder mapping tests across .NET composite formatting, ICU, printf, i18next interpolation, Android XML, and XLIFF inline codes
- plural mapping tests for gettext, Android, Apple, ARB, and i18next
- source-generator compile tests over importer-produced catalogs
- analyzer tests for imported catalogs
- command-line or service tests for fail/continue policies
- Uno app build validation on the documented target head
- Uno app UI smoke tests for import review, translation editing, diagnostics, and export review
- docs build after user-facing documentation changes

Documentation validation:

- README and site docs must describe XLIFF as the preferred CAT/TMS exchange format
- README and site docs must describe source-generated ProTranslate catalogs as the preferred app runtime format
- package notes must separate runtime generator/provider behavior from optional import/export tooling
- release notes must not list full format fidelity or complete Uno Translation Studio workflows until validation exists
