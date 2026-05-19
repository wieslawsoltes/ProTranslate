---
title: Migration Guide
description: Move existing XAML applications from direct resource lookup or ad hoc localization to ProTranslate.
---

# Migration Guide

This guide covers migrating an existing Avalonia, WPF, .NET MAUI, WinUI, or Uno application to ProTranslate. The recommended migration is incremental: introduce the core services first, migrate low-risk labels, then move dynamic formatting and settings screens.

## 1. Inventory Existing Localization

Identify:

- Existing `.resx` resources.
- Existing `IStringLocalizer` usage.
- JSON, PO, database, or remote translation stores.
- Direct `ResourceManager.GetString` calls.
- View-model properties that return localized strings.
- XAML labels that are static by key.
- Format strings that combine text, numbers, dates, currency, or region values.
- Existing culture switching logic.
- Existing flow-direction handling.

Keep the current resource store at first. ProTranslate can sit in front of it through a provider.

## 2. Add Packages

Shared project:

```xml
<PackageReference Include="ProTranslate.Abstractions" Version="0.1.0-preview.1" />
<PackageReference Include="ProTranslate.Core" Version="0.1.0-preview.1" />
```

ResourceManager workflow:

```xml
<PackageReference Include="ProTranslate.ResourceManager" Version="0.1.0-preview.1" />
```

Microsoft.Extensions workflow:

```xml
<PackageReference Include="ProTranslate.MicrosoftExtensions" Version="0.1.0-preview.1" />
```

Application project:

```xml
<PackageReference Include="ProTranslate.Avalonia" Version="0.1.0-preview.1" />
```

Use the matching adapter package for WPF, MAUI, WinUI, or Uno.

## 3. Register Core Services

With dependency injection:

```csharp
services.AddProTranslate(culture: CultureInfo.GetCultureInfo("en-US"));
```

Without dependency injection:

```csharp
var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
var provider = new InMemoryTranslationProvider()
    .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Title", "Dashboard");

var translations = new TranslationService(provider, cultures);
```

Then connect the adapter bridge:

```csharp
ProTranslate.Avalonia.TranslationService.UseService(translations, cultures);
```

Use the corresponding adapter namespace for WPF, MAUI, WinUI, or Uno.

## 4. Migrate Static XAML Text

Before:

```xml
<TextBlock Text="Orders" />
```

After in Avalonia or WPF with prefix-free namespace support:

```xml
<TextBlock Text="{Translate Orders.Title}" />
```

After in portable explicit syntax:

```xml
<TextBlock xmlns:pt="https://github.com/protranslate/xaml"
           Text="{pt:Translate Orders.Title}" />
```

WinUI:

```xml
<TextBlock xmlns:pt="using:ProTranslate.WinUI"
           Text="{pt:Translate Orders.Title}" />
```

Start with read-only labels, headers, menu text, and empty-state copy.

## 5. Migrate Formatted Values

Move simple formatting to `ITranslationService.Format`:

```csharp
public string TotalDisplay => _translations.Format("Orders.Total", Total);
```

Use adapter `Format` only where the target framework supports the binding pattern cleanly. Avalonia, WPF, and MAUI have stronger multi-binding support than WinUI and Uno. For WinUI and Uno, prefer `x:Bind` or view-model properties for dynamic formatted values.

## 6. Add Runtime Culture Switching

Before migrating every view, introduce one application-level culture command:

```csharp
public void UseCulture(string cultureName)
{
    _cultures.SetCulture(cultureName);
}
```

Then validate:

- Existing translated labels update.
- Formatted values update.
- Date, number, and currency displays update.
- Flow direction updates for RTL cultures.
- View-model properties raise change notifications where they compose translated text.

## 7. Add Region And Measurement Policy

Use `IGlobalizationService` when UI depends on region or measurement preferences:

```csharp
globalization.SetRegionOverride(new RegionInfo("GB"));
globalization.SetMeasurementSystemOverride(MeasurementSystem.Metric);
```

Use `IUnitConversionService` and `ILocalizedUnitFormatter` for values that must follow user unit preferences:

```csharp
string distance = globalization.FormatMeasurementForProfile(
    42.2,
    MeasurementUnit.Kilometer,
    "N1");
```

## 8. Introduce Generated Keys

Add a catalog file:

```xml
<ItemGroup>
  <AdditionalFiles Include="Localization/Strings.en-US.json" />
  <AdditionalFiles Include="Localization/App.protranslate.keys.txt" />
</ItemGroup>
```

Use generated keys in view models:

```csharp
using ProTranslate.Generated;

public string Title => _translations.Value_ShellTitle();
```

Generated keys reduce typo risk and give compiled binding and `x:Bind` scenarios a stable path.

## 9. Framework-Specific Notes

Avalonia:

- Prefix-free syntax is supported through the Avalonia default namespace mapping.
- Use compiled binding-friendly view-model properties for dynamic text.

WPF:

- Prefix-free syntax is supported through the WPF presentation namespace mapping.
- `Translation.Culture` also updates culture-related native state such as `XmlLanguage`.

MAUI:

- Use `xmlns:pt="https://github.com/protranslate/xaml"`.
- MAUI does not allow third-party additions to its protected default namespace.

WinUI:

- Use `xmlns:pt="using:ProTranslate.WinUI"`.
- Prefer `x:Bind` or view-model properties for dynamic formatted values.

Uno:

- Use `xmlns:pt="using:ProTranslate.Uno"` for WinUI-compatible syntax.
- Shared URI mapping depends on Uno tooling support.

## 10. Cutover Checklist

- Core services are registered in the composition root.
- Adapter bridge is configured at application startup.
- Existing provider is wrapped or migrated.
- Static XAML labels use `Translate` or `T`.
- Dynamic formatted text uses `Format` or view-model properties.
- Culture switching is centralized.
- Region and measurement preferences are explicit.
- Generated keys are enabled for shared view-model code.
- `ProTranslate.Analyzers` is enabled for static key and catalog validation.
- Missing-key diagnostics are monitored.
- Framework samples or equivalent app screens are validated.
- Documentation for application localization policy is updated.

## Common Pitfalls

- Do not place provider fallback logic in adapter code.
- Do not assume MAUI or WinUI support prefix-free third-party XAML extensions.
- Do not ignore `CultureServiceOptions` when tests or hosts require thread-culture isolation.

The deeper working migration guide remains in [docs/spec/migration-guide.md](https://github.com/wieslawsoltes/ProTranslate/blob/main/docs/spec/migration-guide.md).
