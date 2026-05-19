---
title: Getting Started
description: Build the first ProTranslate-enabled XAML screen.
---

# Getting Started

This guide takes a new application from package references to a translated XAML view. It uses the current preview surface: synchronous providers, service-driven culture switching, XAML markup extensions, and framework-specific adapter registration.

## Choose The Packages

Most applications need one core package and one adapter package:

| Scenario | Packages |
| --- | --- |
| Framework-neutral service layer | `ProTranslate.Core` |
| `.resx` or satellite assembly resources | `ProTranslate.ResourceManager` |
| Microsoft.Extensions dependency injection | `ProTranslate.MicrosoftExtensions` |
| Avalonia UI | `ProTranslate.Avalonia` |
| WPF UI | `ProTranslate.Wpf` |
| .NET MAUI UI | `ProTranslate.Maui` |
| WinUI UI | `ProTranslate.WinUI` |
| Uno Platform UI | `ProTranslate.Uno` |
| Strongly typed keys, bindable strings, generated provider code, and accessors | `ProTranslate.SourceGenerator` |

Use only the adapter for the framework that owns the XAML project. Adapters are intentionally independent and should not reference each other.

## Create A Provider

Use `InMemoryTranslationProvider` while integrating the framework. Replace it later with `ResourceManagerTranslationProvider`, `StringLocalizerTranslationProvider`, or an application provider.

```csharp
using System.Globalization;
using ProTranslate;

var provider = new InMemoryTranslationProvider()
    .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Title", "Orders")
    .Add(CultureInfo.GetCultureInfo("pl-PL"), "Shell.Title", "Zamowienia");
```

Missing keys return the key by default and report diagnostics through `LocalizedString.Diagnostics` and `ITranslationService.DiagnosticReported`.

## Register Services With DI

For applications using Microsoft.Extensions:

```csharp
using Microsoft.Extensions.DependencyInjection;
using ProTranslate;

var services = new ServiceCollection();

services.AddProTranslate(
    provider: provider,
    culture: CultureInfo.GetCultureInfo("en-US"));
```

Add the adapter registration for the framework:

```csharp
services.AddProTranslateAvalonia();
services.AddProTranslateWpf();
services.AddProTranslateMaui();
services.AddProTranslateWinUI();
services.AddProTranslateUno();
```

Call only the adapter method for the current UI project, then bootstrap that adapter after building the service provider:

```csharp
using ServiceProvider serviceProvider = services.BuildServiceProvider();
serviceProvider.UseProTranslateAvalonia();
```

Use the matching `UseProTranslateWpf`, `UseProTranslateMaui`, `UseProTranslateWinUI`, or `UseProTranslateUno` method for the active UI framework.

## Register Services Without DI

Applications can also construct the services directly:

```csharp
var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
var translations = new TranslationService(provider, cultures);
```

Then connect the adapter binding source:

```csharp
ProTranslate.Avalonia.TranslationService.UseService(translations, cultures);
```

Use the matching adapter namespace for WPF, MAUI, WinUI, or Uno.

## Add XAML

Avalonia and WPF can use prefix-free markup once the adapter assembly is referenced:

```xml
<TextBlock Text="{Translate Shell.Title}" />
```

MAUI should use the shared ProTranslate URI because the MAUI default namespace is protected:

```xml
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:pt="https://github.com/protranslate/xaml">
  <Label Text="{pt:Translate Shell.Title}" />
</ContentPage>
```

WinUI uses `using:` syntax:

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:pt="using:ProTranslate.WinUI">
  <TextBlock Text="{pt:T Shell.Title}" />
</Window>
```

Uno samples keep WinUI-compatible syntax:

```xml
xmlns:pt="using:ProTranslate.Uno"
```

## Switch Culture

Inject or keep a reference to `ICultureService` and set the active culture:

```csharp
public sealed class SettingsViewModel
{
    private readonly ICultureService _cultures;

    public SettingsViewModel(ICultureService cultures)
    {
        _cultures = cultures;
    }

    public void UsePolish()
    {
        _cultures.SetCulture("pl-PL");
    }
}
```

The adapter binding source raises property changes for translated values and culture-aware binding paths. View models should still raise their own property notifications for derived text they compute themselves.

## Validate

Run the portable validation first:

```bash
./build.sh
```

Then build the sample for the target framework. For example:

```bash
dotnet build samples/ProTranslate.Avalonia.Sample/ProTranslate.Avalonia.Sample.csproj -c Release
```

Use [XAML Namespaces](xaml-namespaces.md), [Providers](providers.md), and the relevant framework guide next.
