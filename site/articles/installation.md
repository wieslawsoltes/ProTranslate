---
title: Installation
description: Package selection and setup patterns for ProTranslate applications.
---

# Installation

ProTranslate is split into small packages so applications can keep UI framework dependencies out of the core localization layer. Install the framework-neutral packages in shared projects and the adapter package only in the XAML application project.

## Package Map

| Package | Use When |
| --- | --- |
| `ProTranslate.Abstractions` | You need only interfaces and result contracts in a shared project. |
| `ProTranslate.Core` | You need culture state, lookup, fallback, cache policy, formatting, unit conversion, diagnostics, and in-memory providers. |
| `ProTranslate.ResourceManager` | You load translations from `.resx` or satellite assemblies. |
| `ProTranslate.MicrosoftExtensions` | You use `IServiceCollection` or `IStringLocalizer`. |
| `ProTranslate.SourceGenerator` | You want generated key constants, `ITranslationService` accessors, compiled-binding-friendly string properties, generated JSON provider code, and provider manifests. |
| `ProTranslate.Analyzers` | You want build-time validation of static keys, placeholders, and catalog coverage. |
| `ProTranslate.Avalonia` | Your XAML UI is Avalonia. |
| `ProTranslate.Wpf` | Your XAML UI is WPF. |
| `ProTranslate.Maui` | Your XAML UI is .NET MAUI. |
| `ProTranslate.WinUI` | Your XAML UI is WinUI / Windows App SDK. |
| `ProTranslate.Uno` | Your XAML UI is Uno Platform. |

## Shared Project Pattern

Use this pattern when the view models live in a framework-neutral project:

- Reference `ProTranslate.Abstractions` or `ProTranslate.Core` from the shared project.
- Reference the UI adapter only from the UI project.
- Keep view models dependent on `ICultureService`, `ITranslationService`, or `IGlobalizationService`.
- Keep view-only text in XAML through markup extensions.

## Application Project Pattern

The UI application project usually references:

```xml
<PackageReference Include="ProTranslate.Core" Version="..." />
<PackageReference Include="ProTranslate.MicrosoftExtensions" Version="..." />
<PackageReference Include="ProTranslate.Avalonia" Version="..." />
```

Replace `ProTranslate.Avalonia` with the adapter for the current framework.

## Dependency Injection

`ProTranslate.MicrosoftExtensions` registers the core services:

```csharp
services.AddProTranslate(
    provider: provider,
    culture: CultureInfo.GetCultureInfo("en-US"),
    cultureOptions: new CultureServiceOptions
    {
        ApplyToCurrentThread = false,
        ApplyToDefaultThread = false
    },
    cacheOptions: new TranslationCacheOptions
    {
        MaximumEntries = 2048
    });
```

The adapter registration connects the static binding source used by XAML markup extensions:

```csharp
services.AddProTranslateAvalonia();
```

The current preview does not provide a fluent options builder for provider packages. Construct providers and `TranslationFallbackOptions` directly.

## Direct Construction

Direct construction is useful for small apps, tests, and apps with custom composition:

```csharp
var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
var translations = new TranslationService(provider, cultures);
var globalization = new GlobalizationService(cultures, translations);
```

Connect the adapter manually:

```csharp
ProTranslate.Wpf.TranslationService.UseService(translations, cultures);
```

## Source Generator Setup

Add `ProTranslate.SourceGenerator` as an analyzer package and include AdditionalFiles:

```xml
<PackageReference Include="ProTranslate.SourceGenerator"
                  Version="..."
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />

<AdditionalFiles Include="Resources/Strings.*.json" />
<AdditionalFiles Include="Resources/App.protranslate.keys.txt" />
```

Generated output includes `ProTranslate.Generated.ProTranslateKeys`, `ProTranslate.Generated.ProTranslateAccessors`, `ProTranslate.Generated.ProTranslateStrings`, and `ProTranslate.Generated.ProTranslateGeneratedTranslationProvider`.
It also includes `ProTranslate.Generated.ProTranslateProviderManifest` for deterministic catalog metadata. Use `ProTranslateStrings` or equivalent view-model properties as the default path for Avalonia compiled bindings and WinUI/Uno `x:Bind`; use the generated provider when JSON catalogs should not be loaded through runtime resource discovery.

## Analyzer Setup

Add `ProTranslate.Analyzers` as a private analyzer reference in application projects that contain ProTranslate usage:

```xml
<PackageReference Include="ProTranslate.Analyzers"
                  Version="..."
                  PrivateAssets="all"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Use the same `AdditionalFiles` catalog items that feed the source generator. The analyzer reports `PTA001` through `PTA005` for missing static keys, placeholder mismatches, culture catalog coverage gaps, unsafe dynamic keys, and invalid catalogs.

## Build Requirements

- The repository targets .NET 10.
- MAUI builds require MAUI workloads.
- WPF and WinUI real app paths require Windows for normal CI validation.
- Uno sample validation is Windows-oriented in the current preview.

See [Testing](testing.md) and [CI And Release](ci-release.md) for validation commands.
