---
title: WPF Guide
description: Use ProTranslate in WPF applications.
---

# WPF Guide

`ProTranslate.Wpf` integrates core translations with WPF markup extensions, dependency properties, `XmlLanguage`, and native `FlowDirection`.

## Setup

Reference the WPF adapter from the WPF application:

```xml
<PackageReference Include="ProTranslate.Wpf" Version="..." />
```

Connect services at startup:

```csharp
ProTranslate.Wpf.TranslationService.UseService(translations, cultures);
```

With Microsoft.Extensions:

```csharp
services.AddProTranslateWpf();
```

## XAML Namespace

WPF supports prefix-free ProTranslate syntax because the adapter maps into the WPF presentation namespace:

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <TextBlock Text="{Translate AppTitle}" />
</Window>
```

The explicit shared URI is also available:

```xml
xmlns:pt="https://github.com/protranslate/xaml"
```

## Culture And Flow Direction

Set culture and automatic flow direction at a view root:

```xml
<Grid Translation.Culture="{Binding Strings.Culture}"
      Translation.AutoFlowDirection="True">
  <TextBlock Text="{Translate AppTitle}" />
</Grid>
```

The adapter updates WPF `Language`/`XmlLanguage` and maps ProTranslate direction to WPF `FlowDirection`.

## Formatting

WPF `FormatExtension` supports bound `Value` through `MultiBinding`:

```xml
<TextBlock Text="{Format Orders.Total, Value={Binding Total}}" />
```

Use view-model properties for more complex text that depends on multiple model values or unit conversions.

## Attached Key API

```xml
<TextBlock Translation.Key="Orders.Empty"
           Translation.FallbackValue="No orders"
           Translation.StringFormat="{}{0}" />
```

The attached key path is useful when a style or template needs to assign translation behavior through dependency properties instead of a markup extension.

## Validation

WPF sample builds are validated on Windows CI:

```bash
dotnet build samples/ProTranslate.Wpf.Sample/ProTranslate.Wpf.Sample.csproj -c Release
```

On non-Windows systems the project is inspectable but real WPF app execution remains Windows-oriented.

## Gotchas

- WPF design-time behavior is not exhaustively validated in the preview.
- Dispatcher and detached-target retention tests remain planned.
- Prefix-free namespace usage is convenient, but `xmlns:pt="https://github.com/protranslate/xaml"` is safer if the app has another type named `Translate`, `Format`, or `Translation`.
