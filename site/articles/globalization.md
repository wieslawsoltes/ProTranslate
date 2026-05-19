---
title: Globalization
description: Use culture switching, region profiles, measurement-system profiles, overrides, and flow direction in ProTranslate.
---

# Globalization

`IGlobalizationService` combines culture state, translation lookup, region metadata, measurement-system policy, and framework-neutral flow direction. The service lives in the core and does not reference Avalonia, WPF, MAUI, WinUI, or Uno.

```csharp
var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
var translations = new TranslationService(provider, cultures);
var globalization = new GlobalizationService(cultures, translations);
```

Use the adapter packages to map this core state into native XAML bindings and native `FlowDirection` values.

## Culture State

`ICultureService` exposes:

- `CurrentCulture` for number, date, currency, and `string.Format` behavior.
- `CurrentUICulture` for translation lookup.
- `CurrentRegion` derived from the formatting culture.
- `FlowDirection` derived from the UI culture.
- `CultureChanged` with old and new formatting/UI culture snapshots.

```csharp
cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));

cultures.SetCulture(
    CultureInfo.GetCultureInfo("pl-PL"),
    CultureInfo.GetCultureInfo("en-US"));
```

Calling `SetCulture` with the same formatting culture and UI culture is idempotent and does not raise a change event.

`CultureService` construction captures the initial culture without changing ambient thread state. By default, explicit `SetCulture` calls apply active cultures to `CultureInfo.DefaultThreadCurrentCulture`, `CultureInfo.DefaultThreadCurrentUICulture`, `Thread.CurrentThread.CurrentCulture`, and `Thread.CurrentThread.CurrentUICulture`. Hosts that need culture isolation can opt out:

```csharp
var cultures = new CultureService(
    CultureInfo.GetCultureInfo("en-US"),
    new CultureServiceOptions
    {
        ApplyToCurrentThread = false,
        ApplyToDefaultThread = false
    });
```

## RegionProfile

`RegionProfile` is an immutable snapshot over .NET `RegionInfo` plus the culture that was used to resolve it.

```csharp
RegionProfile region = globalization.RegionProfile;

string regionCode = region.TwoLetterISORegionName;
string nativeName = region.NativeName;
string currency = region.ISOCurrencySymbol;
bool isMetric = region.IsMetric;
```

The default provider resolves regions from the formatting culture. Neutral cultures are converted to a specific culture before constructing `RegionInfo`; invariant culture falls back to `RegionInfo.CurrentRegion` in `CultureService`.

Use `SetRegionOverride` when the user wants language and region policy to differ:

```csharp
globalization.SetRegionOverride(new RegionInfo("US"));
globalization.ClearRegionOverride();
```

A region override affects `RegionProfile`, `MeasurementSystem`, `MeasurementSystemProfile`, and `IsMetric`. It does not change `CurrentCulture`, `CurrentUICulture`, or provider lookup.

## MeasurementSystemProfile

`MeasurementSystemProfile` describes default unit labels for the resolved measurement policy:

```csharp
MeasurementSystemProfile profile = globalization.MeasurementSystemProfile;

string length = profile.LengthUnit;
string temperature = profile.TemperatureUnit;
string mass = profile.MassUnit;
string volume = profile.VolumeUnit;
string speed = profile.SpeedUnit;
```

The default resolver maps regions as follows:

| Region | Measurement system | Profile |
| --- | --- | --- |
| `US`, `LR`, `MM` | `USCustomary` | `ft`, `F`, `lb`, `gal`, `mph` |
| `GB` | `Imperial` | `ft`, `C`, `st`, `imp gal`, `mph` |
| metric regions | `Metric` | `m`, `C`, `kg`, `l`, `km/h` |
| remaining regions | `Custom` | empty unit labels |

Use `SetMeasurementSystemOverride` when user preference should win over region defaults:

```csharp
globalization.SetMeasurementSystemOverride(MeasurementSystem.Metric);
globalization.ClearMeasurementSystemOverride();
```

Overrides are evaluated when the profile is read. Setting or clearing region and measurement overrides raises `IGlobalizationService.CultureChanged` with the current formatting and UI cultures on both sides of the event payload. Use that event as the single refresh signal for culture and globalization preference changes.

## Unit Conversion And Localized Units

`IUnitConversionService` converts values across supported length, temperature, mass, volume, and speed units:

```csharp
double miles = globalization.UnitConverter.Convert(
    42.2,
    MeasurementUnit.Kilometer,
    MeasurementUnit.Mile);
```

`ILocalizedUnitFormatter` applies culture-aware number formatting and unit symbols:

```csharp
string display = globalization.UnitFormatter.Format(
    miles,
    MeasurementUnit.Mile,
    globalization.CurrentCulture,
    "N1");
```

For profile-driven display, use the combined helpers:

```csharp
MeasurementValue converted = globalization.ConvertMeasurement(100, MeasurementUnit.Kilometer);
string text = globalization.FormatMeasurementForProfile(100, MeasurementUnit.Kilometer, "N1");
```

Applications can replace either service in DI for domain-specific unit policies.

## Flow Direction

Core flow direction is represented by `TextFlowDirection`:

```csharp
TextFlowDirection direction = globalization.FlowDirection;
```

The value is derived from `CurrentUICulture.TextInfo.IsRightToLeft`. For example, `ar-SA` and `he-IL` resolve to `RightToLeft`; `pl-PL` resolves to `LeftToRight`.

Adapters map core or culture state to native framework values:

- Avalonia: `Avalonia.Media.FlowDirection`
- WPF: `System.Windows.FlowDirection`
- MAUI: `Microsoft.Maui.FlowDirection`
- WinUI and Uno: `Microsoft.UI.Xaml.FlowDirection`

In XAML, enable automatic direction updates through attached properties:

```xml
<Grid pt:Translation.Culture="{Binding Culture}"
      pt:Translation.AutoFlowDirection="True" />
```

When `Translation.Culture` changes, adapters set the shared ProTranslate culture service. If `AutoFlowDirection` is enabled, they also update the native flow-direction property for the target element. WPF and WinUI also update the native language metadata on framework elements.

## Culture Switching In UI

Markup extensions bind through `TranslationBindingSource`. When the core translation service raises `CultureChanged`, the binding source raises `PropertyChanged` for `Culture` and `Item[]`, which refreshes indexed translation bindings.

Attached `Translation.Key` paths subscribe weakly to binding-source changes and refresh supported text/content controls:

- Avalonia: `TextBlock`, `TextBox`, `ContentControl`
- WPF: `TextBlock`, `TextBox`, `HeaderedContentControl`, `ContentControl`
- MAUI: `Label`, `Button`, `Entry`, `Editor`, `SearchBar`
- WinUI and Uno: `TextBlock`, `ContentControl`

Avalonia runtime automation validates translated text refresh, formatted value refresh, attached translation properties, culture switching, automatic RTL flow direction, and release-only leak paths for disposed adapter targets. WPF, MAUI, WinUI, and Uno still rely primarily on build validation and manual/runtime host testing for broader dispatcher behavior. Keep long-lived application services in DI and dispose custom binding sources when you create them manually.
