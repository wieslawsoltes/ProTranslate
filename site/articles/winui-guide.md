---
title: WinUI Guide
description: Use ProTranslate in WinUI and Windows App SDK applications.
---

# WinUI Guide

`ProTranslate.WinUI` integrates translations with WinUI markup extensions, dependency properties, `x:Bind`-friendly view models, and native flow direction.

## Setup

Reference the adapter from the WinUI application:

```xml
<PackageReference Include="ProTranslate.WinUI" Version="..." />
```

Connect services:

```csharp
ProTranslate.WinUI.TranslationService.UseService(translations, cultures);
```

With Microsoft.Extensions:

```csharp
services.AddProTranslateWinUI();
```

## XAML Namespace

WinUI does not expose an assembly-level `XmlnsDefinitionAttribute` for adapter libraries. Use `using:`:

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:pt="using:ProTranslate.WinUI">
  <TextBlock Text="{pt:T AppTitle}" />
</Window>
```

## `x:Bind`

The WinUI sample uses `x:Bind` for strongly typed view-model properties. Static catalog text comes from generated `ProTranslateStrings` exposed through the view model:

```xml
<TextBlock Text="{x:Bind ViewModel.Strings.AppTitle, Mode=OneWay}" />
```

Use `pt:T` for concise view-only labels. Prefer generated `ProTranslateStrings`, generated constants, or generated accessors for strongly typed key paths in code and for trimming-sensitive views.

## Culture And Flow Direction

```xml
<StackPanel pt:Translation.Culture="{x:Bind ViewModel.Strings.Culture, Mode=OneWay}"
            pt:Translation.AutoFlowDirection="True">
  <TextBlock Text="{pt:T AppTitle}" />
</StackPanel>
```

## Formatting

WinUI does not provide the same native `MultiBinding` surface as Avalonia, WPF, and MAUI. The adapter supports static `Value` formatting in `FormatExtension`, while dynamic formatted text should normally stay in `x:Bind` view-model properties:

```xml
<TextBlock Text="{x:Bind ViewModel.InvoiceTotalText, Mode=OneWay}" />
```

## Validation

WinUI sample builds are validated on Windows CI:

```bash
dotnet build samples/ProTranslate.WinUI.Sample/ProTranslate.WinUI.Sample.csproj -c Release
```

Non-Windows builds use a stub project path so the repository can restore and inspect on macOS/Linux.

## Gotchas

- Use `using:ProTranslate.WinUI`; the shared URI is not supported by Windows App SDK assembly attributes.
- UI automation and runtime platform testing are planned beyond the current sample build.
- Keep view-model text notifications explicit when `x:Bind` consumes derived values.
