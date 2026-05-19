---
title: FAQ
description: Common questions about ProTranslate packages, validation, XAML usage, providers, source generation, and preview limitations.
---

# FAQ

## What is ProTranslate?

ProTranslate is a preview XAML translation and globalization framework with a framework-neutral core and thin adapters for Avalonia, WPF, .NET MAUI, WinUI, and Uno Platform. The core owns culture state, provider fallback, formatting, diagnostics, region profile metadata, measurement-system mapping, and flow-direction decisions.

## Which packages are available?

The preview package set is `ProTranslate.Abstractions`, `ProTranslate.Core`, `ProTranslate.ResourceManager`, `ProTranslate.MicrosoftExtensions`, `ProTranslate.SourceGenerator`, `ProTranslate.Analyzers`, `ProTranslate.Avalonia`, `ProTranslate.Wpf`, `ProTranslate.Maui`, `ProTranslate.WinUI`, and `ProTranslate.Uno`.

## Is the core tied to a UI framework?

No. `ProTranslate.Core` and `ProTranslate.Abstractions` are framework-neutral. Adapters are responsible only for native XAML integration, binding refresh, attached properties, native flow-direction mapping, and framework-specific setup.

## How do I run the main validation checks?

Use:

```bash
./build.sh
./build-docs.sh
```

`./build.sh` restores and builds `ProTranslate.CI.slnx`, then runs the core, analyzer, and Avalonia adapter test projects. `./build-docs.sh` performs a clean Lunet documentation build.

## How are platform checks split?

Portable projects and tests run on Linux, macOS, and Windows CI. Avalonia adapter tests also run across those operating systems. Avalonia and MAUI sample builds are validated on macOS CI, with MAUI requiring the MAUI workload. WPF, WinUI, and Uno sample builds are validated on Windows CI.

## How do I build packages locally?

Use:

```bash
./pack.sh 0.1.0-preview.local
```

The output goes to `artifacts/packages`. `pack.sh` discovers packable projects under `src`, requires `.nupkg` and `.snupkg` outputs, and validates analyzer/source-generator package layout.

## Which XAML namespace should I use?

Avalonia and WPF can use prefix-free sample syntax after the adapter assembly is referenced, and they also support:

```xml
xmlns:pt="https://github.com/protranslate/xaml"
```

MAUI should use the shared ProTranslate URI. WinUI should use:

```xml
xmlns:pt="using:ProTranslate.WinUI"
```

Uno samples use:

```xml
xmlns:pt="using:ProTranslate.Uno"
```

because that keeps the sample WinUI-compatible even though the adapter also emits a shared URI mapping for Uno tooling that honors it.

## Does runtime culture switching update UI?

Yes for the implemented preview paths. Culture switching updates translated values, formatted values through the translation service, binding-source refresh paths, attached translation properties, and automatic flow direction where the adapter supports it. Avalonia has runtime UI and release-only leak coverage; broader dispatcher automation for WPF, MAUI, WinUI, and Uno remains platform hardening work.

## Does ProTranslate mutate thread cultures?

By default, `CultureService` applies active cultures to `Thread.CurrentThread` and default thread cultures. Set `CultureServiceOptions.ApplyToCurrentThread` and `ApplyToDefaultThread` to `false` when a host needs isolation.

## Which providers are supported?

The preview supports in-memory providers, composite providers, `ResourceManager`, and `IStringLocalizer`. Custom providers can implement `ITranslationProvider`.

## What happens when a key is missing?

Missing keys return the key by default, set `LocalizedString.ResourceNotFound`, and report structured diagnostics. `ProTranslate.Analyzers` can report missing static keys at build time with `PTA001`.

## How does formatting work?

`ITranslationService.Format` uses `string.Format` with the active formatting culture. Invalid format strings or placeholder mismatches report structured diagnostics. `IUnitConversionService` and `ILocalizedUnitFormatter` cover built-in unit conversion and culture-aware unit text; custom pluralization/list formatters remain extension points.

## Does the source generator support compiled binding and `x:Bind` scenarios?

Yes. `ProTranslate.SourceGenerator` emits key constants, get/value/format/observe accessors, `ProTranslateStrings` CLR properties, generated JSON provider code, and provider manifests from `*.protranslate.keys.txt`, `Strings.*.json`, and `*.protranslate.json`. WinUI and Uno samples keep dynamic formatting in strongly typed view-model or `x:Bind` paths where native XAML multi-binding is unavailable.

## Are analyzer diagnostics available?

Yes. `ProTranslate.Analyzers` reports `PTA001` missing static keys, `PTA002` placeholder mismatches, `PTA003` resource coverage gaps, `PTA004` unsafe dynamic keys, and `PTA005` invalid catalogs.

## Are region and measurement services complete?

The preview includes `RegionProfile`, `MeasurementSystem`, `MeasurementSystemProfile`, region overrides, measurement-system overrides, default measurement mapping, unit conversion, and localized unit formatting. Current samples use the core services for distance and temperature display.

## How do docs get published?

The docs workflow builds the Lunet site with `./build-docs.sh`, uploads `site/.lunet/build/www` as the GitHub Pages artifact, and deploys on `main`.

## How do releases get published?

The release workflow runs for `v*` tags or manual dispatch. It builds, tests, packs, uploads artifacts, and publishes to NuGet for tag builds or manual runs with `publish_nuget=true`. Publishing requires the `NUGET_API_KEY` secret.
