---
title: Translation Formats
description: Import/export support, CAT/TMS exchange guidance, source-generator runtime catalogs, and the Uno Translation Studio direction.
---

# Translation Formats

ProTranslate is designed to fit professional localization workflows without making application runtime code parse every external file format. The preferred workflow is:

1. Exchange with translators, CAT tools, or TMS systems through XLIFF.
2. Import external files into normalized ProTranslate catalogs with diagnostics.
3. Use source-generated ProTranslate catalogs for application runtime.
4. Export back to exchange or platform-native formats when needed.

This page describes optional import/export tooling and authoring guidance. The current preview source generator reads `*.protranslate.keys.txt`, `Strings.*.json`, and `*.protranslate.json`. Do not treat the broader format list below as runtime provider behavior; exchange files should be imported into normalized ProTranslate catalogs first.

## Recommended Formats

Use XLIFF for CAT/TMS handoff. XLIFF 1.2 remains common in enterprise tooling, while XLIFF 2.1 is the preferred modern exchange format when the toolchain supports it.

Use source-generated ProTranslate catalogs for app runtime. The generator emits key constants, accessors, `ProTranslateStrings`, provider code, and manifests that work well with compiled bindings, WinUI/Uno `x:Bind`, trimming, and CI.

Use CSV or TSV for spreadsheet review only when the workflow needs it. Spreadsheet exchange is practical, but it has weaker metadata, escaping, and type-safety guarantees than XLIFF.

## Format Coverage

| Format | Use it for | Notes |
| --- | --- | --- |
| XLIFF 1.2 | Legacy CAT/TMS exchange. | Preserve source, target, notes, state, context, and inline placeholders where supported. |
| XLIFF 2.1 | Modern CAT/TMS exchange. | Preferred external exchange format for new professional workflows. |
| gettext PO/POT | Open-source translation teams and template extraction. | Preserve comments, references, contexts, plurals, and fuzzy state where possible. |
| .NET RESX | Visual Studio and `ResourceManager` interoperability. | String resources map cleanly; non-string resources need diagnostics. |
| Android `strings.xml` | Android and MAUI Android handoff. | Strings, arrays, and plurals need XML and quantity-rule validation. |
| Apple `.strings` | iOS and macOS string resources. | Key/value strings and comments are straightforward; encoding and duplicates need diagnostics. |
| Apple `.stringsdict` | Apple plural dictionaries. | Plural variables and categories need explicit mapping. |
| Apple `.xcstrings` | Xcode string catalogs. | Localization states and variations need a careful normalized model. |
| Flutter ARB | Flutter Intl projects. | Descriptions, placeholders, plurals, and selects need ICU-aware validation. |
| i18next JSON | Web and JavaScript applications. | Nested keys, namespaces, interpolation, and plural suffixes need configurable mapping. |
| CSV/TSV | Spreadsheet, vendor, or product review. | Require explicit columns, encoding, duplicate-key checks, and formula-safety handling. |

## Loss-Aware Import And Export

Format conversion must be loss-aware. External formats have different ideas of plurals, placeholder syntax, workflow state, comments, references, namespaces, and inline markup. ProTranslate tooling should report unsupported constructs instead of silently dropping them.

Diagnostics should identify:

- source format and file
- key or external unit ID
- culture
- unsupported construct
- whether data was preserved, approximated, ignored, or requires review
- suggested repair or safer target format

Examples:

- importing a RESX file with binary resources should preserve string entries and report non-string resources as unsupported
- exporting ARB ICU plurals to CSV should mark plural metadata as needing review unless the CSV schema has plural columns
- importing XLIFF inline codes should validate placeholder identity before generating ProTranslate catalogs

## Source Generator Runtime Path

The source generator consumes key files and normalized ProTranslate catalogs. Professional workflows should import CAT/TMS, platform-native, or spreadsheet exchange files through `ProTranslate.Formats` or Uno Translation Studio first, review any format-specific loss diagnostics, then commit normalized runtime catalogs.

Current generator inputs:

```xml
<ItemGroup>
  <AdditionalFiles Include="Resources\Strings.*.json" />
  <AdditionalFiles Include="Resources\*.protranslate.json" />
  <AdditionalFiles Include="Resources\*.protranslate.keys.txt" />
</ItemGroup>
```

Generated outputs:

- `ProTranslateKeys`
- `ProTranslateAccessors`
- `ProTranslateStrings`
- `ProTranslateGeneratedTranslationProvider`
- `ProTranslateProviderManifest`

The generated CLR surface is the recommended path for Avalonia compiled bindings and WinUI/Uno `x:Bind`.

## Uno Translation Studio

The Uno Translation Studio app is an authoring and review tool over the same format services used by command-line tooling. It is not part of the Uno adapter runtime surface.

Expected workflow:

- import XLIFF, PO/POT, RESX, Android, Apple, ARB, i18next JSON, CSV, or TSV files
- preview normalized records and import diagnostics
- edit source and target strings in a dense translation grid
- filter by culture, missing keys, fuzzy state, review state, diagnostics, and placeholder issues
- validate placeholders and plural metadata before save or export
- export XLIFF for CAT/TMS exchange or source-generator-ready ProTranslate catalogs for app runtime

Uno-specific guidance:

- keep translated app text on generated `ProTranslateStrings` or view-model properties for `x:Bind`
- use `pt:T` for concise view-only markup and migration scenarios
- keep translation authoring UI separate from `ProTranslate.Core` and `ProTranslate.Uno`

## Implementation Checklist

Before docs or package notes claim full support for a format, validation should include:

- parser and writer tests
- round-trip tests
- loss diagnostics tests
- culture tag normalization tests
- placeholder and plural mapping tests
- source-generator compile tests over importer output
- analyzer tests over importer output
- Uno app build and UI smoke validation for documented target heads

The detailed working spec is [Translation Formats And Uno App](https://github.com/wieslawsoltes/ProTranslate/blob/main/docs/spec/translation-formats-and-uno-app.md) in the repository spec set.
