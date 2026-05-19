# Package Release Notes

These notes describe the current preview package contents. They are intentionally factual and exclude planned features until they are implemented.

## Preview Scope

Implemented packages:
- `ProTranslate.Abstractions`
- `ProTranslate.Core`
- `ProTranslate.ResourceManager`
- `ProTranslate.MicrosoftExtensions`
- `ProTranslate.SourceGenerator`
- `ProTranslate.Analyzers`
- `ProTranslate.Avalonia`
- `ProTranslate.Wpf`
- `ProTranslate.Maui`
- `ProTranslate.WinUI`
- `ProTranslate.Uno`
- `ProTranslate.Formats`

## Package Notes

### `ProTranslate.Abstractions`

- Defines culture, translation, globalization, provider, observable localized string, measurement-system, and flow-direction contracts.
- Defines structured diagnostics, region profile, measurement-system profile, and failure-policy contracts.
- Defines `LocalizedString` with missing-resource, provider-name, and diagnostic metadata.

### `ProTranslate.Core`

- Provides `CultureService`, `TranslationService`, `GlobalizationService`, `InMemoryTranslationProvider`, and `CompositeTranslationProvider`.
- Supports parent/default fallback through `TranslationFallbackOptions`.
- Supports `Format(key, args)` through .NET `string.Format`.
- Supports translation lookup cache policy and explicit invalidation.
- Supports thread-culture opt-out through `CultureServiceOptions`.
- Supports observable localized strings for binding-friendly view-model properties.
- Supports structured diagnostics for missing translations, provider failures, and format failures.
- Supports reusable region profile, measurement-system resolution, unit conversion, and localized unit formatting with explicit overrides.
- Culture switching can update current/default thread cultures or stay isolated based on host options.

### `ProTranslate.ResourceManager`

- Provides `ResourceManagerTranslationProvider`.
- Handles missing manifest and satellite assembly misses as missing resources.

### `ProTranslate.MicrosoftExtensions`

- Provides `AddProTranslate`.
- Provides `AddProTranslateStringLocalizer<TResource>`.
- Provides `StringLocalizerTranslationProvider`.

### Adapter Packages

Avalonia, WPF, MAUI, WinUI, and Uno packages provide:
- `TranslateExtension`
- `TExtension`
- `FormatExtension`
- `FExtension`
- XML namespace mappings where supported by the target framework
- adapter `TranslationBindingSource`
- static adapter `TranslationService`
- attached `Translation.Key`
- attached `Translation.FallbackValue`
- attached `Translation.StringFormat`
- attached `Translation.Culture`
- attached `Translation.AutoFlowDirection`
- DI helper named `AddProTranslate{Framework}`

### `ProTranslate.SourceGenerator`

- Reads additional files named `*.protranslate.keys.txt`, `Strings.*.json`, and `*.protranslate.json`.
- Emits `ProTranslate.Generated.ProTranslateKeys` constants and `ProTranslateAccessors` get/value/format/observe helpers.
- Emits `ProTranslate.Generated.ProTranslateStrings` for compiled binding and `x:Bind`.
- Emits `ProTranslate.Generated.ProTranslateGeneratedTranslationProvider` for JSON catalog values.
- Emits `ProTranslate.Generated.ProTranslateProviderManifest` with key, culture, source file, and placeholder metadata.
- Handles identifier normalization and stable hash collision suffixes.
- Reports invalid JSON and duplicate-key generator warnings.

### `ProTranslate.Analyzers`

- Reports missing static key diagnostics.
- Reports placeholder count mismatches.
- Reports culture catalog coverage gaps.
- Reports unsafe dynamic key usage.
- Reports invalid catalog additional files.

### `ProTranslate.Formats`

- Provides normalized `TranslationCatalog` and `TranslationCatalogEntry` models for translation exchange.
- Provides `TranslationCatalogConverter` import/export APIs.
- Provides initial parser/writer surfaces for ProTranslate JSON, i18next JSON, Flutter ARB, RESX, Android `strings.xml`, Apple `.strings`, Apple `.stringsdict`, Apple `.xcstrings`, gettext PO/POT, XLIFF 1.2, XLIFF 2.x workflows, CSV, and TSV.
- Reports format diagnostics for invalid or unsupported input constructs.
- Remains optional tooling infrastructure; application runtime lookup should prefer source-generated ProTranslate catalogs or explicitly registered providers.

### Uno Translation Studio Sample

- Provides a professional Uno authoring sample under `samples/ProTranslate.Uno.TranslationStudio`.
- Demonstrates a compact review UI, source/target editing, review-state commands, import/export previews, generated `ProTranslateStrings` for `x:Bind`, and normalized ProTranslate catalog output for the runtime/source-generator path.
- Remains a sample and validation target, not part of the `ProTranslate.Uno` runtime adapter package.

## Known Limitations

- Logging/debug-overlay integrations over structured diagnostics are not implemented.
- Analyzer code fixes are not implemented.
- Rich provider trace and cache hit/miss diagnostics are not implemented.
- Broad WPF, MAUI, WinUI, and Uno runtime UI automation is not implemented.
- Full fidelity and round-trip validation for every industry format construct remains hardening work.
- The Uno Translation Studio sample needs documented build validation and UI smoke coverage before release notes claim full authoring workflow validation.
- Deeper dispatcher and retained-target stress tests beyond current Avalonia coverage are not implemented.
- WinUI and Uno runtime paths are Windows-oriented in CI.
- WinUI does not support assembly-level `XmlnsDefinitionAttribute`; WinUI examples use `using:ProTranslate.WinUI`.

## Validation For This Preview

- Portable solution build and tests run in CI across Linux, macOS, and Windows.
- Core and source-generator tests run in `tests/ProTranslate.Tests`.
- Analyzer tests run in `tests/ProTranslate.Analyzers.Tests`.
- Avalonia adapter smoke, runtime, and leak tests run in `tests/ProTranslate.Avalonia.Tests`.
- Docs build through Lunet.
- Avalonia sample build plus MAUI adapter build and sample restore are validated on macOS CI; full MAUI sample app packaging is validated locally on a configured Mac.
- WPF, WinUI, and Uno sample builds are validated on Windows CI.

## Future Package Notes

Format release notes must state which formats are implemented and validated. XLIFF should be described as the preferred CAT/TMS exchange format. Source-generated ProTranslate catalogs should remain the recommended runtime path for applications. Any unsupported or lossy import/export constructs must be documented as diagnostics, not as silent conversion behavior.
