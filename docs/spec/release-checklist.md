# ProTranslate Release Checklist

Use this checklist for preview package releases. Keep it aligned with the implemented API surface and keep remaining hardening work separate from shipped behavior.

## Inputs

- release version, usually `0.1.0-preview.<number>` until the API stabilizes
- current `main` branch or a tagged release commit
- package artifacts from `./pack.sh`
- current README, specs, site pages, and sample validation notes
- NuGet API key only when publishing

## Outputs

- `.nupkg` and `.snupkg` artifacts under `artifacts/packages`
- GitHub Actions release artifact or GitHub release for tags
- package notes that list implemented packages and known limitations
- migration guide updates when public usage changes

## Constraints

- package versions must stay aligned across released packages
- `ProTranslate.Core` and `ProTranslate.Abstractions` must remain framework-neutral
- MAUI packaging requires a machine with the MAUI workload
- Windows-specific sample app paths are validated in Windows CI
- release notes must identify planned work separately from shipped behavior
- format import/export and the Uno Translation Studio app must not be listed with broader support claims than their parser/writer, diagnostics, generator, analyzer, and app validations justify

## Release Steps

1. Update docs before packaging:
   - `README.md`
   - `docs/spec/api-surface.md`
   - `docs/spec/validation-matrix.md`
   - `docs/spec/translation-formats-and-uno-app.md` when format tooling or the Uno authoring app changes
   - `docs/spec/package-release-notes.md`
   - `docs/spec/migration-guide.md`
   - matching `site/articles/*` pages

2. Run local validation where the machine supports it:

   ```bash
   ./build.sh
   ./build-docs.sh
   dotnet build samples/ProTranslate.Avalonia.Sample/ProTranslate.Avalonia.Sample.csproj -c Release
   dotnet build samples/ProTranslate.Maui.Sample/ProTranslate.Maui.Sample.csproj -c Release
   ```

3. Let CI validate platform coverage:
   - portable solution on Linux, macOS, and Windows
   - docs build
   - Avalonia and MAUI sample builds on macOS
   - WPF, WinUI, and Uno sample builds on Windows

4. Pack with an explicit version:

   ```bash
   ./pack.sh 0.1.0-preview.<number>
   ```

5. Inspect package contents:
   - `.nupkg` exists for each intended package
   - `.snupkg` exists for symbol packages
   - package readme is included
   - package descriptions match implemented services
   - analyzer/source-generator packages deliver DLLs under `analyzers/dotnet/cs`
   - analyzer/source-generator packages do not include runtime `lib` DLLs or package dependencies

6. Publish only after CI is green:
   - use the release workflow with `publish_nuget=false` for artifact review
   - use a `v*` tag or manual workflow input with `publish_nuget=true` when publishing

## Edge Cases

- If MAUI workload installation fails, release portable packages and document that MAUI packaging was not produced.
- If Windows sample CI fails, do not claim WPF, WinUI, or Uno sample validation for that release.
- If docs build fails, fix docs before package publication.
- If an adapter lags core changes, call it out in package notes instead of removing package boundaries.

## Validate

- `./build.sh`
- `./build-docs.sh`
- sample build matrix in `.github/workflows/ci.yml`
- release workflow artifact upload
- package metadata inspection
