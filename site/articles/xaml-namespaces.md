---
title: XAML Namespaces
description: XML namespace mapping and prefix guidance for every supported XAML framework.
---

# XAML Namespaces

ProTranslate exposes the same XAML concepts across frameworks, but each XAML compiler has different namespace rules. The adapter packages provide the best supported mapping for their target framework.

## Support Matrix

| Framework | Recommended Syntax | Prefix-Free Support | Notes |
| --- | --- | --- | --- |
| Avalonia | Prefix-free or `xmlns:pt="https://github.com/protranslate/xaml"` | Yes | Adapter maps into Avalonia's default XML namespace. |
| WPF | Prefix-free or `xmlns:pt="https://github.com/protranslate/xaml"` | Yes | Adapter maps into WPF's default presentation namespace. |
| MAUI | `xmlns:pt="https://github.com/protranslate/xaml"` | No | MAUI protects its default namespace and rejects third-party mappings. |
| WinUI | `xmlns:pt="using:ProTranslate.WinUI"` | No | Windows App SDK does not expose an assembly-level `XmlnsDefinitionAttribute`. |
| Uno | `xmlns:pt="using:ProTranslate.Uno"` | Not portable | Adapter emits a shared URI mapping for Uno tooling, but samples keep WinUI-compatible syntax. |

## Prefix-Free Avalonia

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <StackPanel Translation.Culture="{Binding Strings.Culture}"
              Translation.AutoFlowDirection="True">
    <TextBlock Text="{Translate AppTitle}" />
  </StackPanel>
</Window>
```

This works because `ProTranslate.Avalonia` maps `ProTranslate.Avalonia` into `https://github.com/avaloniaui`.

## Prefix-Free WPF

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <StackPanel Translation.Culture="{Binding Strings.Culture}"
              Translation.AutoFlowDirection="True">
    <TextBlock Text="{Translate AppTitle}" />
  </StackPanel>
</Window>
```

This works because `ProTranslate.Wpf` maps `ProTranslate.Wpf` into the WPF presentation namespace.

## Shared ProTranslate URI

Use this syntax when you want an explicit prefix or when the framework requires one:

```xml
xmlns:pt="https://github.com/protranslate/xaml"
```

MAUI uses this form:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:pt="https://github.com/protranslate/xaml">
  <VerticalStackLayout pt:Translation.Culture="{Binding Strings.Culture}">
    <Label Text="{pt:T AppTitle}" />
  </VerticalStackLayout>
</ContentPage>
```

Avalonia and WPF also support the shared URI if your team prefers explicit prefixes.

## WinUI

WinUI uses `using:`:

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:pt="using:ProTranslate.WinUI">
  <TextBlock Text="{pt:T AppTitle}" />
</Window>
```

Do not document or depend on assembly-level `XmlnsDefinition` for WinUI. The Windows App SDK exposes XML namespace metadata through generated `IXamlMetadataProvider` infrastructure, not an assembly attribute that adapter libraries can apply.

## Uno

Uno samples use WinUI-compatible syntax:

```xml
xmlns:pt="using:ProTranslate.Uno"
```

The adapter also emits `XmlnsDefinition` metadata for `https://github.com/protranslate/xaml` for Uno tooling that honors it. Treat that as an Uno-specific convenience, not as portable XAML shared with WinUI.

## Naming Guidance

Use `Translate` and `Format` in prefix-free docs because they are clearer than short aliases:

```xml
<TextBlock Text="{Translate AppTitle}" />
```

Use `T` and `F` when the code needs compact explicit prefix syntax:

```xml
<TextBlock Text="{pt:T AppTitle}" />
```

The short aliases are implemented, but `Translate` and `Format` are better for public examples and reduce future collision risk in default XML namespaces.
