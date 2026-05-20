---
title: .NET MAUI Guide
description: Use ProTranslate in .NET MAUI applications.
---

# .NET MAUI Guide

`ProTranslate.Maui` adapts translations to MAUI markup extensions, bindable attached properties, binding-source refresh, and native flow direction.

## Setup

Reference the adapter from the MAUI application:

```xml
<PackageReference Include="ProTranslate.Maui" Version="..." />
```

Register services in `MauiProgram`:

```csharp
services.AddProTranslate(
    provider: provider,
    culture: CultureInfo.GetCultureInfo("en-US"));

services.AddProTranslateMaui();

using ServiceProvider serviceProvider = services.BuildServiceProvider();
serviceProvider.UseProTranslateMaui();
```

Or connect directly:

```csharp
ProTranslate.Maui.TranslationService.UseService(translations, cultures);
```

## XAML Namespace

MAUI protects `http://schemas.microsoft.com/dotnet/2021/maui` and rejects third-party assemblies in that default namespace. Use the shared ProTranslate URI:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:pt="https://github.com/protranslate/xaml">
  <Label Text="{pt:Translate AppTitle}" />
</ContentPage>
```

## Culture And Flow Direction

```xml
<ContentPage pt:Translation.Culture="{Binding Strings.Culture}"
             pt:Translation.AutoFlowDirection="True">
  <VerticalStackLayout>
    <Label Text="{pt:T AppTitle}" />
  </VerticalStackLayout>
</ContentPage>
```

The adapter maps ProTranslate flow direction to MAUI `FlowDirection`.

## Formatting

MAUI `FormatExtension` supports bound `Value` through `MultiBinding`:

```xml
<Label Text="{pt:Format Orders.Total, Value={Binding Total}}" />
```

MAUI XamlC has stricter compile-time rules than desktop XAML stacks. Keep examples close to the sample and build with the target workload installed.

## Validation

On macOS with MAUI workloads:

```bash
dotnet build samples/ProTranslate.Maui.Sample/ProTranslate.Maui.Sample.csproj -c Release
```

CI validates the Mac Catalyst path on macOS. Other platform heads require the relevant workload and lifecycle testing.

## Gotchas

- Do not map ProTranslate into MAUI's default XML namespace.
- The current validation path builds a Mac Catalyst sample locally when workloads are installed.
- Mobile lifecycle reactivation tests are planned.
- Keep complex formatted text in view models when it depends on multiple fields or user unit preferences.
