---
title: MVVM Guide
---

# MVVM Guide

ProTranslate is designed so view models can stay independent from Avalonia, WPF, .NET MAUI, WinUI, and Uno. The core abstractions live in the `ProTranslate` namespace, and framework adapters only translate those abstractions into XAML markup extensions, attached properties, native `FlowDirection`, and binding refresh behavior.

Use XAML for view-only text. Use view-model properties when the text is part of application state, participates in command workflows, requires compiled binding or `x:Bind`, or combines translated strings with domain values.

## Choose The Right Surface

| Scenario | Recommended surface |
| --- | --- |
| Static label in a view | `T`/`Translate` markup extension. |
| Static formatted label with supported native binding shape | `F`/`Format` markup extension. |
| Text attached to existing controls | `Translation.Key`, `Translation.FallbackValue`, and `Translation.StringFormat`. |
| Culture-aware shell direction | `Translation.Culture` plus `Translation.AutoFlowDirection`. |
| View-model text with commands or domain state | `ITranslationService`, generated accessors, or a typed proxy. |
| Region or measurement display | `IGlobalizationService` or a view-model service that wraps it. |
| Diagnostics display or logging bridge | `DiagnosticReported` or `IProTranslateDiagnosticSink`. |

Representative XAML:

```xml
<TextBlock Text="{Translate Shell.FileMenu}" />
<TextBlock Translation.Key="Orders.EmptyState"
           Translation.FallbackValue="No orders" />
<Grid Translation.Culture="{Binding Strings.Culture}"
      Translation.AutoFlowDirection="True" />
```

MAUI uses the shared ProTranslate XML namespace prefix, while WinUI uses `using:` syntax:

```xml
<ContentPage xmlns:pt="https://github.com/protranslate/xaml"
             pt:Translation.AutoFlowDirection="True" />
```

```xml
<StackPanel xmlns:pt="using:ProTranslate.WinUI"
            pt:Translation.AutoFlowDirection="True" />
```

## Keep View Models Framework-Neutral

A portable view model should depend on abstractions, not adapter packages:

```csharp
public sealed class CulturePickerViewModel
{
    private readonly IGlobalizationService _globalization;

    public CulturePickerViewModel(IGlobalizationService globalization)
    {
        _globalization = globalization;
    }

    public CultureInfo CurrentCulture => _globalization.CurrentCulture;
    public RegionProfile Region => _globalization.RegionProfile;
    public MeasurementSystemProfile Units => _globalization.MeasurementSystemProfile;

    public void UsePolish() =>
        _globalization.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));
}
```

Do not reference `ProTranslate.Avalonia`, `ProTranslate.Wpf`, `ProTranslate.Maui`, `ProTranslate.WinUI`, or `ProTranslate.Uno` from shared view-model projects. Adapters belong at the app or view layer.

## Culture Switching

Runtime culture switching flows through `ICultureService` and is observed by `ITranslationService`, `IGlobalizationService`, observable strings, and adapter binding sources.

```csharp
public sealed class ShellViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ITranslationService _translations;

    public ShellViewModel(ITranslationService translations)
    {
        _translations = translations;
        _translations.CultureChanged += OnCultureChanged;
    }

    public string Title => _translations.GetString("Shell.Title").Value;

    public void Dispose() =>
        _translations.CultureChanged -= OnCultureChanged;

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

Use `IObservableLocalizedString` when the localized value itself is the object you want to bind:

```csharp
public sealed class GreetingViewModel : IDisposable
{
    private readonly IObservableLocalizedString _greeting;

    public GreetingViewModel(ITranslationService translations)
    {
        _greeting = translations.Observe("Greeting", "Marta");
    }

    public string Greeting => _greeting.Value;

    public void Dispose() => _greeting.Dispose();
}
```

Observable strings subscribe to culture changes. Dispose them from long-lived view models, view-model collections, or navigation caches.

## Formatting In View Models

`ITranslationService.GetString` uses the active UI culture for lookup. `ITranslationService.Format` formats with the active formatting culture:

```csharp
public string InvoiceTotalText =>
    _translations.Format("Invoice.Total", InvoiceTotal);
```

This separation is important for applications that display one language but format numbers, dates, or currency for another region. Format failures produce structured diagnostics and return the unformatted localized value by default unless `TranslationFormatFailureBehavior.ReportAndThrow` is configured.

## Region And Measurement State

Use `IGlobalizationService` when a view model owns region or unit policy:

```csharp
public void UseUnitedStatesRegion()
{
    _globalization.SetRegionOverride(new RegionInfo("US"));
    OnPropertyChanged(nameof(RegionName));
    OnPropertyChanged(nameof(UnitSystem));
}

public string RegionName => _globalization.RegionProfile.NativeName;
public MeasurementSystem UnitSystem => _globalization.MeasurementSystem;
```

The core resolves `US`, `LR`, and `MM` to `USCustomary`, `GB` to `Imperial`, metric regions to `Metric`, and remaining regions to `Custom`. Use `IUnitConversionService` and `ILocalizedUnitFormatter` for display values that depend on user unit preferences; the samples use these services for distance and temperature text.

## Compiled Binding And `x:Bind`

Compiled binding systems need real CLR members. Prefer one of these patterns:

- Generated `ProTranslateStrings` properties for static text.
- Generated key constants and `ProTranslateAccessors` wrapped by view-model properties for custom naming or composition.
- A typed strings proxy that raises `PropertyChanged` when culture changes.
- Plain view-model properties for formatted text that combines domain values with translation output.

Avalonia samples enable compiled bindings with `x:DataType`. WinUI and Uno samples use `x:Bind` against strongly typed view-model members:

```xml
<TextBlock Text="{x:Bind ViewModel.Strings.AppTitle, Mode=OneWay}" />
<TextBlock Text="{x:Bind ViewModel.InvoiceTotalText, Mode=OneWay}" />
```

This avoids reflection-only dynamic lookup paths and keeps trimming and AOT behavior easier to reason about.

For new code, treat adapter markup extensions as concise XAML convenience APIs. Use generated CLR properties or view-model properties as the default path when the target framework has compiled binding, `x:Bind`, NativeAOT, or strict trimming requirements.

## SOLID Boundaries

- Single responsibility: providers resolve text, core services select culture and fallback, view models coordinate state, adapters adapt to XAML.
- Open/closed: add providers, region providers, measurement resolvers, or diagnostic sinks through interfaces.
- Liskov substitution: custom providers should return `LocalizedString` with the same missing-key and diagnostic semantics as built-in providers.
- Interface segregation: depend on `ITranslationService` for text, `ICultureService` for culture switching, `IGlobalizationService` for region and measurement policy, and diagnostic abstractions for reporting.
- Dependency inversion: shared application code depends on `ProTranslate.Abstractions`, not a UI framework package.

## Practical Rules

- Keep literal view chrome in XAML when markup extensions are enough.
- Keep command-driven language, region, and unit preferences in view models or application services.
- Raise `PropertyChanged` for every computed localized property affected by culture, region, measurement, or argument changes.
- Do not duplicate fallback or formatting policy in view models.
- Do not mutate thread culture directly from view models; use the ProTranslate culture service and configure `CultureServiceOptions` when a host needs current/default thread culture isolation.
