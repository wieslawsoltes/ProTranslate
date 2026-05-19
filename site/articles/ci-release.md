---
title: CI and Release
description: GitHub Actions workflow behavior, local release preparation, package artifacts, and publishing rules.
---

# CI and Release

The repository uses GitHub Actions for cross-platform validation, documentation publishing, preview package artifacts, and release publishing. Keep release notes factual: shipped behavior belongs in release notes, while remaining hardening work such as analyzer code fixes, richer provider traces, logging/debug overlays, and non-Avalonia runtime UI automation belongs in limitations.

## CI Workflow

`.github/workflows/ci.yml` runs on pushes to `main` and `develop`, pull requests targeting `main`, and manual dispatch.

The `build-test` job runs on `ubuntu-latest`, `macos-latest`, and `windows-latest`:

```bash
dotnet restore ProTranslate.CI.slnx
dotnet build ProTranslate.CI.slnx -c Release --no-restore -p:ContinuousIntegrationBuild=true
dotnet test tests/ProTranslate.Tests/ProTranslate.Tests.csproj -c Release --no-build
dotnet test tests/ProTranslate.Analyzers.Tests/ProTranslate.Analyzers.Tests.csproj -c Release --no-build
dotnet test tests/ProTranslate.Avalonia.Tests/ProTranslate.Avalonia.Tests.csproj -c Release --no-build
```

Test results are uploaded as short-retention artifacts for each operating system.

The `docs-build` job runs `./build-docs.sh` on Ubuntu.

The `sample-builds` job waits for build/test and docs. It validates the Avalonia sample plus the MAUI adapter and sample restore on macOS, installing the MAUI workload first, and validates WPF, WinUI, and the Windows-hosted Uno sample on Windows. Uno heads pin the modern `win-x64` Windows App SDK RID, reference the centrally managed Windows App SDK version, enable the WinUI XAML toolchain, and let the generated WinUI entry point own startup so .NET 10 does not fall back to legacy `win10-*` runtime identifiers, old XAML compiler tasks, or missing `InitializeComponent` output during CI restore/build.

The `pack-preview` job runs on macOS after build/test and sample builds. It installs the MAUI workload, runs `./pack.sh 0.1.0-ci.<run_number>`, and uploads `artifacts/packages` as `nuget-preview-packages`.

## Docs Workflow

`.github/workflows/docs.yml` runs on pushes to `main` that affect docs, site content, README, the docs build script, the local tool manifest, or the docs workflow. It can also be started manually.

The workflow builds the site with:

```bash
./build-docs.sh
```

It uploads `site/.lunet/build/www` as the GitHub Pages artifact and deploys it to the `github-pages` environment when the ref is `refs/heads/main`.

## Release Workflow

`.github/workflows/release.yml` runs for `v*` tags and manual dispatch. Manual runs accept:

- `version`: package version, defaulting to `0.1.0-preview.<run_number>` when omitted.
- `publish_nuget`: whether to publish packages to NuGet from a manual run.

The release job runs on macOS, installs the MAUI workload, resolves the version, runs:

```bash
./build.sh
./pack.sh "<version>"
```

It uploads package artifacts as `nuget-packages-<version>`. Publishing to NuGet happens for tags or when `publish_nuget` is true. Publishing requires `NUGET_API_KEY`; the workflow fails explicitly when the secret is missing.

For tag builds, the workflow also creates a GitHub release with generated release notes and attaches `.nupkg` and `.snupkg` files.

## Local Release Preparation

Before starting a release workflow, update public docs that describe shipped behavior:

- `README.md`
- `docs/spec/api-surface.md`
- `docs/spec/validation-matrix.md`
- `docs/spec/package-release-notes.md`
- `docs/spec/migration-guide.md`
- matching `site/articles/*` pages

Then run the local checks supported by the current machine:

```bash
./build.sh
./build-docs.sh
dotnet build samples/ProTranslate.Avalonia.Sample/ProTranslate.Avalonia.Sample.csproj -c Release
dotnet build samples/ProTranslate.Maui.Sample/ProTranslate.Maui.Sample.csproj -c Release
./pack.sh 0.1.0-preview.local
```

MAUI validation and packaging require installed MAUI workloads. Windows-specific sample validation should come from Windows CI unless you are on a Windows machine.

## Package Artifacts

The preview package set is:

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

After packing, inspect `artifacts/packages` for every intended `.nupkg` and `.snupkg`. `pack.sh` discovers packable projects under `src`, validates README inclusion, and checks analyzer/source-generator packages for `analyzers/dotnet/cs` delivery without runtime `lib` DLLs or package dependencies.

## Release Rules

Use preview versions until the API stabilizes. Keep package versions aligned across released packages. Do not publish packages unless CI is green, docs build successfully, package metadata is inspected, and release notes separate shipped behavior from planned work.

If Windows sample CI fails, do not claim WPF, WinUI, or Uno sample validation for that release. If docs fail, fix docs before publishing packages. If an adapter lags a core change, call it out in package notes rather than weakening package boundaries.
