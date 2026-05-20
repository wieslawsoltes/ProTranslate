---
title: Uno Platform Guide
description: Use ProTranslate in Uno Platform applications.
---

# Uno Platform Guide

`ProTranslate.Uno` follows WinUI-style APIs while keeping the adapter assembly separate for Uno Platform applications.

The adapter is runtime XAML integration. Uno Translation Studio is a separate authoring surface for import/export, translation editing, diagnostics review, and source-generator-ready catalog output.

## Setup

Reference the adapter from the Uno application:

```xml
<PackageReference Include="ProTranslate.Uno" Version="..." />
```

Connect services:

```csharp
ProTranslate.Uno.TranslationService.UseService(translations, cultures);
```

With Microsoft.Extensions:

```csharp
services.AddProTranslateUno();

using ServiceProvider serviceProvider = services.BuildServiceProvider();
serviceProvider.UseProTranslateUno();
```

## XAML Namespace

The sample keeps WinUI-compatible syntax:

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:pt="using:ProTranslate.Uno">
  <TextBlock Text="{pt:T AppTitle}" />
</Window>
```

The adapter also emits a shared `https://github.com/protranslate/xaml` mapping for Uno tooling that honors `XmlnsDefinition`, but that form is Uno-specific and not shared with WinUI.

## `x:Bind`

Use `x:Bind` for strongly typed properties. Static catalog text can bind directly to generated `ProTranslateStrings` exposed through the view model:

```xml
<TextBlock Text="{x:Bind ViewModel.Strings.AppTitle, Mode=OneWay}" />
```

Use `pt:T` for simple view-only labels. Prefer generated `ProTranslateStrings`, generated constants, or generated accessors for strongly typed key paths in code and for trimming-sensitive views.

## Culture And Flow Direction

```xml
<StackPanel pt:Translation.Culture="{x:Bind ViewModel.Strings.Culture, Mode=OneWay}"
            pt:Translation.AutoFlowDirection="True">
  <TextBlock Text="{pt:T AppTitle}" />
</StackPanel>
```

## Formatting

The Uno adapter mirrors the WinUI adapter shape. `FormatExtension` preserves `Value={Binding ...}` by returning that binding with a formatting converter; view-model properties consumed by `x:Bind` remain the preferred path for complex formatted text.

## Validation

Uno sample builds use `Uno.Sdk` desktop heads and are validated by the Windows sample job:

```bash
dotnet build samples/ProTranslate.Uno.Sample/ProTranslate.Uno.Sample.csproj -c Release
```

The desktop build path is intentionally cross-platform. WebAssembly, mobile, and deeper runtime UI automation are planned beyond the current sample build.

## Translation Studio

Uno Translation Studio should use shared format tooling to import and export XLIFF 1.2/2.1 workflows, gettext PO/POT, RESX, Android `strings.xml`, Apple `.strings`/`.stringsdict`/`.xcstrings`, Flutter ARB, i18next JSON, and CSV/TSV. XLIFF should be the preferred CAT/TMS exchange path, while source-generated ProTranslate catalogs remain the preferred runtime output for applications.

Expected app workflows include import review, a dense translation grid, source/target editing, culture and diagnostics filters, placeholder validation, plural/context review, and export review with loss-aware diagnostics. These workflows are separate from the current Uno adapter runtime sample and need their own validation coverage.

## Gotchas

- Prefer `using:ProTranslate.Uno` in shared WinUI-compatible XAML samples.
- Verify globalization data availability on WebAssembly and mobile heads.
- Keep platform-specific code in partial files or platform-specific projects rather than in shared translation services.
