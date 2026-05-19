# ProTranslate Samples

The samples demonstrate the implemented preview surface across XAML frameworks. They are validation samples, not exhaustive UI test suites.

## Shared Scenario

All framework samples use shared view-model and generated catalog code from `ProTranslate.Samples.Shared`:

- JSON source catalogs for `en-US`, `pl-PL`, and `ar-SA` included as `AdditionalFiles`
- a source-generated `ProTranslateGeneratedTranslationProvider` instead of runtime assembly resource discovery
- live culture switching
- region selection in the sample view model
- metric/imperial display selected by the sample view model through ProTranslate core unit conversion and localized unit formatting services
- formatted order, distance, and temperature text
- generated and handwritten typed CLR properties that work with compiled binding or `x:Bind`

`GeneratedTranslationProxy` composes the generated `ProTranslateStrings` surface with sample-specific region, unit, and formatted-display properties. The source generator emits key constants, get/value/format/observe helpers, bindable strings, generated provider code, and provider manifests from text and JSON catalogs.

## Validation Split

| Sample | Local macOS validation | CI validation | Notes |
| --- | --- | --- | --- |
| `ProTranslate.Avalonia.Sample` | Build locally with `dotnet build samples/ProTranslate.Avalonia.Sample/ProTranslate.Avalonia.Sample.csproj -c Release` | macOS sample job | Uses Avalonia compiled binding and prefix-free `Translate`/`Translation.*` through default XML namespace mapping. |
| `ProTranslate.Maui.Sample` | Build locally with MAUI workloads installed: `dotnet build samples/ProTranslate.Maui.Sample/ProTranslate.Maui.Sample.csproj -c Release` | macOS sample job | Uses the shared `https://github.com/protranslate/xaml` URI because MAUI protects its default namespace. |
| `ProTranslate.Wpf.Sample` | Inspectable on non-Windows; real app path requires Windows | Windows sample job | Validates WPF prefix-free markup extensions, dependency properties, `Language`, and flow-direction sample path. |
| `ProTranslate.WinUI.Sample` | Non-Windows project stub only | Windows sample job | Validates WinUI `x:Bind` view-model paths and attached `Translation.Key` on Windows. |
| `ProTranslate.Uno.Sample` | Non-Windows project stub only | Windows sample job | Validates Windows-hosted Uno `x:Bind` and attached `Translation.Key` paths. The adapter emits a shared URI mapping, while the sample keeps WinUI-compatible `using:` syntax. Skia/WebAssembly runtime validation is planned. |
| `ProTranslate.Uno.TranslationStudio` | Non-Windows project stub only | Windows build validation required | Professional Uno authoring sample for catalog review workflows. Broader UI smoke coverage remains validation hardening work. |

## Uno Translation Studio

`ProTranslate.Uno.TranslationStudio` is a professional authoring and review surface, separate from the adapter validation sample. It is designed to use shared format tooling services, edit normalized ProTranslate catalogs, validate placeholders and plural metadata, and export source-generator-ready catalogs for application runtime.

The sample covers the industry exchange set exposed by `ProTranslate.Formats`: XLIFF 1.2/2.x, gettext PO/POT, RESX, Android `strings.xml`, Apple `.strings`/`.stringsdict`/`.xcstrings`, Flutter ARB, i18next JSON, CSV/TSV, and normalized ProTranslate JSON. XLIFF is the CAT/TMS handoff path; ProTranslate JSON is the runtime/source-generator path.

The runtime samples should continue to demonstrate generated `ProTranslateStrings`, compiled binding or `x:Bind`, culture switching, formatting, region metadata, measurement displays, and flow direction. They should not become catalog authoring tools.

## Local Commands

Core and portable validation:

```bash
./build.sh
```

Docs validation:

```bash
./build-docs.sh
```

Avalonia sample:

```bash
dotnet build samples/ProTranslate.Avalonia.Sample/ProTranslate.Avalonia.Sample.csproj -c Release
```

MAUI sample on macOS with workloads installed:

```bash
dotnet build samples/ProTranslate.Maui.Sample/ProTranslate.Maui.Sample.csproj -c Release
```

Windows-hosted Uno Translation Studio sample:

```bash
dotnet build samples/ProTranslate.Uno.TranslationStudio/ProTranslate.Uno.TranslationStudio.csproj -c Release
```

## Known Gaps

- Analyzer behavior is validated in `tests/ProTranslate.Analyzers.Tests`, not in framework sample apps.
- Samples validate formatted text through view-model paths; adapter smoke tests cover `FormatExtension` on Avalonia.
- Samples validate prefix-free XML namespace usage for Avalonia and WPF, and the shared ProTranslate URI for MAUI.
- Samples use `DefaultUnitConversionService` and `DefaultLocalizedUnitFormatter` for distance and temperature display.
- Avalonia runtime UI automation and release-only leak tests live in `tests/ProTranslate.Avalonia.Tests`; non-Avalonia runtime automation remains platform-specific hardening work.
- The Uno Translation Studio build and deeper UI smoke tests remain validation hardening work.
