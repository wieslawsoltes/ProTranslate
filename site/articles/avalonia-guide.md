---
title: Avalonia Guide
description: Use ProTranslate in Avalonia applications.
---

# Avalonia Guide

`ProTranslate.Avalonia` adapts the core services to Avalonia markup extensions, attached properties, compiled binding-friendly sample paths, and flow-direction updates.

## Setup

Reference the adapter from the Avalonia application:

```xml
<PackageReference Include="ProTranslate.Avalonia" Version="..." />
```

Connect the adapter after the core services are created:

```csharp
ProTranslate.Avalonia.TranslationService.UseService(translations, cultures);
```

If you use Microsoft.Extensions, call the adapter DI helper:

```csharp
services.AddProTranslateAvalonia();

using ServiceProvider serviceProvider = services.BuildServiceProvider();
serviceProvider.UseProTranslateAvalonia();
```

## XAML Namespace

Avalonia supports prefix-free ProTranslate syntax because the adapter maps into `https://github.com/avaloniaui`.

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <TextBlock Text="{Translate AppTitle}" />
</Window>
```

The explicit shared URI is also available:

```xml
xmlns:pt="https://github.com/protranslate/xaml"
```

## Culture And Flow Direction

Set culture and automatic flow direction near the view root:

```xml
<StackPanel Translation.Culture="{Binding Strings.Culture}"
            Translation.AutoFlowDirection="True">
  <TextBlock Text="{Translate AppTitle}" />
</StackPanel>
```

`Translation.Culture` forwards culture changes into the core culture service. `Translation.AutoFlowDirection` maps the framework-neutral `TextFlowDirection` to Avalonia `FlowDirection`.

## Compiled Bindings

The sample enables compiled bindings and uses `x:DataType` for view-model-owned text. Static catalog text comes from the generated `ProTranslateStrings` CLR surface exposed by the sample view model:

```xml
<Window x:DataType="shared:TranslationDemoViewModel">
  <TextBlock Text="{Binding Strings.AppTitle}" />
</Window>
```

For new compiled-binding-first Avalonia code, prefer generated `ProTranslateStrings` or view-model properties for normal text. Use XAML markup extensions for concise view-only labels, migration scenarios, or dynamic cases where a runtime binding path is acceptable.

## Formatting

Avalonia `FormatExtension` supports a bound `Value` through `MultiBinding`:

```xml
<TextBlock Text="{Format Orders.Total, Value={Binding Total}}" />
```

The converter delegates to `TranslationBindingSource.Translate`, which delegates to the core `ITranslationService.Format`.

## Attached Key API

The adapter also supports attached key/fallback/string-format properties:

```xml
<TextBlock Translation.Key="Orders.Empty"
           Translation.FallbackValue="No orders" />
```

The attached key path updates when the culture changes.

## Validation

Run:

```bash
dotnet build samples/ProTranslate.Avalonia.Sample/ProTranslate.Avalonia.Sample.csproj -c Release
dotnet test tests/ProTranslate.Avalonia.Tests/ProTranslate.Avalonia.Tests.csproj -c Release
```

Current automated tests cover binding creation, binding-source refresh, `FormatExtension` shape, attached key refresh, runtime culture switching, formatted value refresh, automatic RTL flow direction, and release-only detached-target leak paths.

## Gotchas

- Keep adapter setup in application startup, not in individual views.
- Prefer `Translate` over `T` in prefix-free default namespace examples.
- Use view-model notifications for text derived from multiple properties.
- Release builds run detached-target leak tests in `tests/ProTranslate.Avalonia.Tests`.
