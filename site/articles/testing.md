---
title: Testing
description: Local and CI validation commands for ProTranslate core, adapters, samples, docs, and packages.
---

# Testing

ProTranslate validation is split between portable checks that can run on any supported developer machine and platform checks that need macOS or Windows framework tooling. Use the repository scripts first, then run the framework-specific sample builds that match the machine you are on.

## Local Baseline

Run the default validation script before opening a pull request:

```bash
./build.sh
```

The script restores `ProTranslate.CI.slnx`, builds it in Release, runs `tests/ProTranslate.Tests`, `tests/ProTranslate.Analyzers.Tests`, and `tests/ProTranslate.Avalonia.Tests`. This covers the framework-neutral core, source generator tests, analyzer diagnostics, provider and formatting behavior, structured diagnostics, unit conversion, and Avalonia adapter smoke/runtime/leak tests.

Run the documentation build whenever site or spec content changes:

```bash
./build-docs.sh
```

The docs script restores local .NET tools, removes `site/.lunet/build`, and runs Lunet from the `site` directory with stack traces enabled.

To view documentation locally:

```bash
./serve-docs.sh
```

`serve-docs.sh` builds the site in development mode, starts Lunet watch mode, and serves `site/.lunet/build/www` at `http://127.0.0.1:8080` by default. Set `DOCS_HOST` or `DOCS_PORT` to override the bind address or port.

## Sample Builds

Build samples that are supported by the current operating system. Sample builds validate XAML namespace mappings, adapter wiring, culture switching paths, flow direction, and strongly typed binding shapes.

Avalonia sample:

```bash
dotnet build samples/ProTranslate.Avalonia.Sample/ProTranslate.Avalonia.Sample.csproj -c Release
```

MAUI sample on macOS with MAUI workloads installed:

```bash
dotnet build samples/ProTranslate.Maui.Sample/ProTranslate.Maui.Sample.csproj -c Release
```

Windows samples on Windows, plus Uno desktop samples on any machine with the .NET SDK and restored Uno packages:

```bash
dotnet build samples/ProTranslate.Wpf.Sample/ProTranslate.Wpf.Sample.csproj -c Release
dotnet build samples/ProTranslate.WinUI.Sample/ProTranslate.WinUI.Sample.csproj -c Release
dotnet build samples/ProTranslate.Uno.Sample/ProTranslate.Uno.Sample.csproj -c Release
dotnet build samples/ProTranslate.Uno.TranslationStudio/ProTranslate.Uno.TranslationStudio.csproj -c Release
```

WinUI remains a Windows-only sample path. Uno samples use `Uno.Sdk` desktop heads and can be built on non-Windows developer machines, while Windows CI keeps them in the hosted sample matrix.

## Package Build

Create local package artifacts with an explicit version:

```bash
./pack.sh 0.1.0
```

Packages are written to `artifacts/packages` by default. The script discovers packable projects under `src`, packs each package, requires `.nupkg` artifacts and runtime `.snupkg` artifacts, checks README inclusion, and validates Roslyn analyzer/source-generator package layout under `analyzers/dotnet/cs`.

## Package Integration

The NuGet package integration workflow validates package consumers from produced artifacts rather than project references:

```bash
.github/workflows/package-integration.yml
```

The workflow packs all packages, restores temporary consumers from the local package feed, runs the portable consumer, verifies analyzer diagnostics from the analyzer package, and builds Avalonia, MAUI, WPF, WinUI, and Uno consumers on the appropriate hosted operating systems.

## Platform Validation Split

| Area | Local expectation | CI expectation |
| --- | --- | --- |
| Portable libraries | `./build.sh` | Linux, macOS, and Windows |
| Core tests | `tests/ProTranslate.Tests` through `./build.sh` | Linux, macOS, and Windows |
| Analyzer tests | `tests/ProTranslate.Analyzers.Tests` through `./build.sh` | Linux, macOS, and Windows |
| Avalonia adapter tests | `tests/ProTranslate.Avalonia.Tests` through `./build.sh` | Linux, macOS, and Windows |
| Docs | `./build-docs.sh` | Ubuntu docs build |
| Avalonia sample | local build where Avalonia SDK dependencies restore | macOS sample job |
| MAUI sample | macOS with MAUI workload | macOS CI builds the MAUI adapter and restores the sample project; full app packaging is local/platform validation |
| WPF sample | Windows only for real app path | Windows sample job |
| WinUI sample | Windows only for real app path | Windows sample job |
| Uno sample | `Uno.Sdk` desktop sample build | Windows sample job; local non-Windows build supported |
| Uno Translation Studio sample | `Uno.Sdk` desktop sample build; UI smoke validation planned | Windows sample job |
| Packages | `./pack.sh <version>`; MAUI requires workload | macOS package and release jobs |
| Package integration | package consumers restored from produced `.nupkg` files | package integration workflow across Ubuntu, macOS, and Windows |

## What To Validate For Feature Work

For core changes, validate provider fallback, cache policy behavior, missing-key diagnostics, format failure behavior, culture switching, thread-culture opt-out, region profile behavior, measurement-system overrides, unit conversion, localized unit formatting, and flow-direction resolution.

For adapter changes, validate the native XAML shape, binding refresh after culture changes, attached `Translation.Key`, `Translation.FallbackValue`, `Translation.StringFormat`, `Translation.Culture`, `Translation.AutoFlowDirection`, and sample builds for the affected framework.

For source generator changes, validate key extraction from `*.protranslate.keys.txt`, `Strings.*.json`, and `*.protranslate.json`; generated constants; get/value/format/observe helpers; `ProTranslateStrings`; generated JSON provider entries; provider manifest entries; invalid JSON warning `PTSG001`; and duplicate-key warning `PTSG002`.

For analyzer changes, validate `PTA001` missing static keys, `PTA002` placeholder mismatches, `PTA003` resource coverage gaps, `PTA004` unsafe dynamic keys, and `PTA005` invalid catalogs.

For docs changes, validate `./build-docs.sh` and keep site articles aligned with the relevant files under `docs/spec/`.

## Current Gaps

The current roadmap still treats logging/debug overlay integrations, WPF/MAUI/WinUI/Uno runtime UI automation, analyzer code fixes, and deeper dispatcher-marshalling stress tests as hardening work. Do not treat sample builds as a substitute for runtime host validation on platform-specific UI stacks.
