---
title: Formatting
description: Format localized strings in ProTranslate core and XAML adapters.
---

# Formatting

ProTranslate separates translation lookup culture from formatting culture. `ITranslationService.GetString` uses `CurrentUICulture`; `ITranslationService.Format` uses `CurrentCulture` when applying .NET formatting.

That split supports scenarios such as English UI text with Polish number and date formatting:

```csharp
var provider = new InMemoryTranslationProvider()
    .Add(CultureInfo.GetCultureInfo("en-US"), "Orders.Total", "Total: {0:N2}");

var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
cultures.SetCulture(
    CultureInfo.GetCultureInfo("pl-PL"),
    CultureInfo.GetCultureInfo("en-US"));

var translations = new TranslationService(provider, cultures);

string text = translations.Format("Orders.Total", 1234.5m);
```

The localized format string is resolved from `en-US`; the numeric value is formatted with `pl-PL`.

## Core Formatting

Core string formatting is intentionally based on .NET composite formatting:

```csharp
string text = translations.Format("Orders.Total", order.Total);
```

The service:

1. Resolves the key with normal provider and culture fallback.
2. Returns the localized value unchanged when no arguments are supplied.
3. Calls `string.Format(CurrentCulture, localized.Value, arguments)` when arguments exist.
4. Reports `FormatFailure` when `string.Format` throws `FormatException`.

Default failure behavior is `ReportAndReturnUnformatted`. The caller receives the localized format string unchanged. Use `ReportAndThrow` when invalid format strings should fail fast:

```csharp
var options = new TranslationFallbackOptions
{
    FormatFailureBehavior = TranslationFormatFailureBehavior.ReportAndThrow
};

var translations = new TranslationService(provider, cultures, options);
```

`Observe(key, arguments)` uses the same translation and formatting path and raises property changes when the culture changes.

## Format Strings

Use standard .NET composite formatting:

```csharp
provider
    .Add(en, "Orders.Total", "Total: {0:C}")
    .Add(pl, "Orders.Total", "Razem: {0:C}");
```

The format item belongs in the localized resource, not in view code, when translators need to move words, punctuation, or values.

Use `StringFormat` on XAML bindings only when the wrapper text is view-owned. For example, wrapping a translated status in brackets is a view concern; changing word order around a currency value is usually a localized resource concern.

## XAML Translate Extensions

`TranslateExtension` and `TExtension` expose translated text as a one-way binding to `TranslationBindingSource`.

```xml
<TextBlock Text="{pt:T Shell.Title}" />
<TextBlock Text="{pt:Translate Shell.Status, StringFormat='{}{0}'}" />
```

`StringFormat` on `TranslateExtension` is a binding-level wrapper around the already translated value. It does not pass arguments into the localized resource.

## XAML Format Extensions

`FormatExtension` and `FExtension` are for localized format strings.

Avalonia, WPF, and MAUI support dynamic `Value` formatting through native multi-binding:

```xml
<TextBlock Text="{pt:F Orders.Total, Value={Binding Total}}" />
```

When `Value` is supplied, the adapter binds to both the translated format string and the value. It then calls `TranslationBindingSource.Translate(key, value)`, which delegates to `ITranslationService.Format`.

When `Value` is omitted, `FormatExtension` behaves like `TranslateExtension` and returns the translated value:

```xml
<TextBlock Text="{pt:F Orders.EmptyState}" />
```

`StringFormat` on `FormatExtension` wraps the final formatted result:

```xml
<TextBlock Text="{pt:F Orders.Total, Value={Binding Total}, StringFormat='{}{0}'}" />
```

## WinUI And Uno Differences

WinUI and Uno do not provide the same native XAML multi-binding surface used by Avalonia, WPF, and MAUI.

In the current WinUI and Uno adapters:

- `TranslateExtension` returns a one-way binding and can apply `StringFormat` through a converter.
- `FormatExtension` can format a static `Value` supplied directly in XAML.
- A bound `Value={Binding ...}` is preserved as the returned binding and receives a formatting converter, so the live bound value is passed into `Translate`.

For complex dynamic formatted text in WinUI and Uno, prefer a view-model property, generated accessor, or `x:Bind` path that calls the core service:

```csharp
public string TotalText => _translations.Format("Orders.Total", Total);
```

This keeps compiled binding and `x:Bind` compatibility without relying on unsupported multi-binding behavior.

## Unit Formatting

`IUnitConversionService` converts supported length, temperature, mass, volume, and speed units. `ILocalizedUnitFormatter` formats the converted number with the selected culture and appends the localized unit symbol.

```csharp
var converter = new DefaultUnitConversionService();
var formatter = new DefaultLocalizedUnitFormatter();
var culture = CultureInfo.GetCultureInfo("pl-PL");

double miles = converter.Convert(42.2, MeasurementUnit.Kilometer, MeasurementUnit.Mile);
string text = formatter.Format(miles, MeasurementUnit.Mile, culture, "N1");
```

`IGlobalizationService` combines conversion and formatting with the active measurement-system profile:

```csharp
string distance = globalization.FormatMeasurementForProfile(
    42.2,
    MeasurementUnit.Kilometer,
    "N1");
```

Samples use these core services for distance and temperature display, then pass the formatted measurement text into localized resource strings.

## Attached StringFormat

Attached translation properties also support a view-owned wrapper format:

```xml
<TextBlock pt:Translation.Key="Orders.EmptyState"
           pt:Translation.StringFormat="{}{0}" />
```

The attached property path resolves the key first. If a fallback value is configured and the key is missing, it uses the fallback value. Then it applies `string.Format(CurrentCulture, stringFormat, value)` to the resolved display value.

## Current Boundaries

The current implementation includes composite string formatting and built-in unit conversion/localized unit formatting. Custom formatter services, pluralization rules, named format profiles, and list formatting remain application extension points.
