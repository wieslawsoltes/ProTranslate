---
title: Release Checklist
description: Step-by-step release preparation for ProTranslate packages, docs, samples, and GitHub Actions.
---

# Release Checklist

Use this checklist before publishing preview or stable ProTranslate packages. Keep release artifacts tied to actual shipped behavior and keep remaining hardening work separate from shipped notes.

## 1. Confirm Scope

- Confirm the release version.
- Confirm whether the release is preview, release candidate, or stable.
- Review merged changes since the last tag.
- Verify public API changes are intentional.
- Verify sample changes match the documented API.
- Verify documentation describes shipped behavior.
- Verify format import/export and Uno Translation Studio claims do not exceed implemented and validated behavior.

## 2. Update Documentation

Update:

- [README](../readme.md).
- [Getting Started](getting-started.md).
- [Installation](installation.md).
- [XAML Namespaces](xaml-namespaces.md).
- [Translation Formats](translation-formats.md) when format tooling or the Uno authoring app changes.
- Framework guides affected by the release.
- [API Surface](api-surface.md).
- [Package Release Notes](package-release-notes.md).
- [Migration Guide](migration-guide.md).
- [FAQ](faq.md).

Build the docs:

```bash
./build-docs.sh
```

## 3. Run Local Validation

Portable validation:

```bash
./build.sh
dotnet format ProTranslate.slnx --verify-no-changes --verbosity minimal
git diff --check
```

Docs validation:

```bash
./build-docs.sh
```

Package validation:

```bash
./pack.sh 0.1.0-releasecheck.local
```

## 4. Validate Samples

macOS:

```bash
dotnet workload install maui
dotnet build samples/ProTranslate.Avalonia.Sample/ProTranslate.Avalonia.Sample.csproj -c Release
dotnet build samples/ProTranslate.Maui.Sample/ProTranslate.Maui.Sample.csproj -c Release
```

Windows:

```powershell
dotnet build samples/ProTranslate.Wpf.Sample/ProTranslate.Wpf.Sample.csproj -c Release
dotnet build samples/ProTranslate.WinUI.Sample/ProTranslate.WinUI.Sample.csproj -c Release
dotnet build samples/ProTranslate.Uno.Sample/ProTranslate.Uno.Sample.csproj -c Release
```

Runtime spot checks:

- Culture switch updates translated text.
- Formatted values update.
- Region display updates.
- Measurement display updates.
- Flow direction updates for RTL cultures.
- Compiled binding or `x:Bind` sample paths still build.

## 5. Inspect Package Artifacts

After packing, inspect `artifacts/packages/`.

Check:

- Expected package names are present.
- `.snupkg` files are present where expected.
- Version is correct.
- License metadata is correct.
- Repository URL is correct.
- README and package descriptions are accurate.
- Dependencies are scoped to the intended package boundaries.
- Roslyn packages deliver analyzer/source-generator DLLs under `analyzers/dotnet/cs`.
- Roslyn packages do not leak runtime `lib` DLLs or package dependencies.

## 6. Verify CI

The `ci.yml` workflow must pass:

- Linux portable build and tests.
- macOS portable build and tests.
- Windows portable build and tests.
- Docs build.
- macOS Avalonia and MAUI sample builds.
- Windows WPF, WinUI, and Uno sample builds.
- Preview package creation.

The `docs.yml` workflow must pass for documentation updates.

## 7. Publish

For a tagged release:

```bash
git tag v0.1.0-preview.1
git push origin v0.1.0-preview.1
```

The release workflow resolves the package version from the tag. Manual workflow dispatch can also publish when `publish_nuget` is set and `NUGET_API_KEY` is configured.

## 8. Post-Release Checks

After publish:

- Confirm NuGet packages are visible.
- Confirm symbols are visible where expected.
- Confirm GitHub release assets include packages.
- Confirm docs deployment succeeded.
- Confirm installation snippets use the released version.
- Open follow-up issues for known limitations that remain.

## Known Preview Limitations To Keep Visible

- Analyzer code fixes are not included.
- Rich provider traces and cache hit/miss diagnostics are not included.
- Logging/debug-overlay integrations are not included.
- WPF, MAUI, WinUI, and Uno runtime UI automation remains platform hardening work.
- Full industry format fidelity remains hardening work until parser/writer, diagnostics, generator, analyzer, and round-trip validation exist for each claimed construct.
- Full Uno Translation Studio workflow coverage remains hardening work until app build and UI smoke validation exist.

The deeper working checklist remains in [docs/spec/release-checklist.md](https://github.com/wieslawsoltes/ProTranslate/blob/main/docs/spec/release-checklist.md).
