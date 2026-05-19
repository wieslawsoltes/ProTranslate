---
title: Troubleshooting
description: Common setup, XAML namespace, provider, source generator, build, docs, and release problems.
---

# Troubleshooting

Start by identifying whether the failure is in the core service pipeline, a framework adapter, source generation, documentation, packaging, or platform tooling. ProTranslate keeps these areas intentionally separate, so a failure in one package should not be fixed by duplicating policy in another adapter.

## XAML Namespace Problems

If `Translate`, `T`, `Format`, `F`, or `Translation.*` cannot be resolved, verify the adapter package reference and the XAML namespace for the target framework.

Avalonia and WPF can use prefix-free sample syntax after the adapter assembly is referenced because those adapters map into the framework default namespace. They also support the shared ProTranslate URI:

```xml
xmlns:pt="https://github.com/protranslate/xaml"
```

MAUI should use the shared URI because its default namespace is protected:

```xml
xmlns:pt="https://github.com/protranslate/xaml"
```

WinUI should use the adapter namespace directly:

```xml
xmlns:pt="using:ProTranslate.WinUI"
```

Uno emits the shared URI mapping for tooling that honors `XmlnsDefinition`, but the sample uses WinUI-compatible syntax:

```xml
xmlns:pt="using:ProTranslate.Uno"
```

If prefix-free markup works in Avalonia or WPF but not in MAUI, WinUI, or Uno, switch to the explicit `pt:` form. That is expected.

## Translations Do Not Update After Culture Changes

Check that the adapter is connected to the same core services used by the application.

With dependency injection, register both the core and the framework adapter helper:

```csharp
services.AddProTranslate(provider, CultureInfo.GetCultureInfo("en-US"));
services.AddProTranslateAvalonia();
```

Use the matching `AddProTranslateWpf`, `AddProTranslateMaui`, `AddProTranslateWinUI`, or `AddProTranslateUno` helper for other frameworks.

Without dependency injection, connect the static adapter binding source:

```csharp
ProTranslate.Avalonia.TranslationService.UseService(translations, cultures);
```

Use the matching static adapter class for the active framework. If XAML uses attached `Translation.Culture` or `Translation.AutoFlowDirection`, verify the property is applied to an element that participates in the relevant visual or logical tree.

## Missing Keys

Missing keys return the key by default and set `LocalizedString.ResourceNotFound`. They are also represented through structured diagnostics. This is the expected production-safe behavior unless the host configures failure behavior differently.

Check these points:

- the key text in XAML matches the provider key exactly
- the active UI culture has a resource or a valid fallback path
- provider order is correct when using `CompositeTranslationProvider`
- `.resx` base names and resource visibility are correct for `ResourceManagerTranslationProvider`
- `IStringLocalizer` is registered for the expected resource type

Enable `ProTranslate.Analyzers` to catch missing static keys at build time with `PTA001`.

## Provider Failures

Provider exceptions can be reported and continued or reported and thrown depending on configured failure behavior. If translations fall back unexpectedly, subscribe to `ITranslationService.DiagnosticReported` or use an `IProTranslateDiagnosticSink` to inspect provider-failure diagnostics.

For `ResourceManager`, verify satellite assemblies, resource base names, culture-specific files, and build actions. For `IStringLocalizer`, verify that Microsoft.Extensions localization is configured before adding the ProTranslate string-localizer provider.

## Format Problems

`ITranslationService.Format` uses `string.Format(CurrentCulture, value, args)`. Invalid placeholders or mismatched arguments produce format-failure diagnostics. By default, the unformatted localized value is returned unless the host opts into throwing.

For dynamic values in XAML, use view-model properties when native binding support is limited. Avalonia, WPF, and MAUI support bound `Value` formatting through native multi-binding. WinUI and Uno keep dynamic formatting in view-model or `x:Bind` paths.

## Source Generator Problems

The source generator reads additional files named:

- `*.protranslate.keys.txt`
- `Strings.*.json`
- `*.protranslate.json`

For text files, use one key per line. Lines beginning with `#` are comments. For JSON catalogs, use valid JSON with flat or nested keys. Invalid JSON reports warning `PTSG001`; duplicate keys in one catalog report warning `PTSG002`.

If generated constants are missing, verify that the catalog file is included as an additional file in the consuming project and that the project references `ProTranslate.SourceGenerator` as an analyzer package or project reference according to the current repository pattern.

If generated identifiers collide, the generator normalizes identifiers and adds stable hash suffixes. Prefer stable, descriptive resource keys to keep generated member names readable.

## Build Problems

Use the narrowest command that matches the failure:

```bash
dotnet restore ProTranslate.CI.slnx
dotnet build ProTranslate.CI.slnx -c Release --no-restore
dotnet test tests/ProTranslate.Tests/ProTranslate.Tests.csproj -c Release --no-build
dotnet test tests/ProTranslate.Analyzers.Tests/ProTranslate.Analyzers.Tests.csproj -c Release --no-build
dotnet test tests/ProTranslate.Avalonia.Tests/ProTranslate.Avalonia.Tests.csproj -c Release --no-build
```

WPF and WinUI require Windows targeting. The repository enables Windows targeting where needed, but real WPF, WinUI, and Windows-hosted Uno sample validation belongs on Windows. Non-Windows stubs allow inspection and portable restore paths; they do not prove the real XAML app path.

MAUI builds require the MAUI workload. Install it before building the MAUI sample or expecting `pack.sh` to produce `ProTranslate.Maui`:

```bash
dotnet workload install maui
```

## Docs Build Problems

Run:

```bash
./build-docs.sh
```

The script restores .NET tools and runs Lunet from the `site` directory. If navigation references a missing article, either create the article or update the menu in a separate documentation task. For this docs site, generated output is under `site/.lunet/build/www`.

To reproduce the local serve path:

```bash
./serve-docs.sh
```

If the default port is busy:

```bash
DOCS_PORT=8081 ./serve-docs.sh
```

## Package And Release Problems

Use:

```bash
./pack.sh 0.1.0-preview.local
```

Package artifacts are written to `artifacts/packages`. The script discovers packable projects under `src`, requires `.nupkg` and `.snupkg` outputs, validates README inclusion, and checks analyzer/source-generator layout. Release publishing requires `NUGET_API_KEY` in GitHub Actions. Tag releases must use `v*` tags, and manual release runs publish to NuGet only when `publish_nuget` is true.

## Known Preview Limitations

Current limitations are not setup errors:

- no analyzer code fixes
- no cache hit/miss diagnostics
- no rich provider trace events
- no logging/debug-overlay integration over diagnostics
- WPF, MAUI, WinUI, and Uno runtime UI automation remains platform hardening work
- WinUI does not support assembly-level `XmlnsDefinitionAttribute`
