---
title: Extensibility
---

# Extensibility

ProTranslate extension points are deliberately framework-neutral. New providers, region policy, measurement policy, and diagnostic sinks plug into the core abstractions and can be reused by Avalonia, WPF, .NET MAUI, WinUI, and Uno applications.

Adapters should remain thin. They should not reimplement provider fallback, formatting, region inference, measurement defaults, or diagnostics policy.

## Translation Providers

Implement `ITranslationProvider` when translations come from a source that is not already covered by `ResourceManagerTranslationProvider`, `StringLocalizerTranslationProvider`, or `InMemoryTranslationProvider`.

```csharp
public sealed class JsonTranslationProvider : ITranslationProvider
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _values;

    public JsonTranslationProvider(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> values)
    {
        _values = values;
    }

    public string Name => "Json";

    public LocalizedString GetString(string key, CultureInfo culture)
    {
        if (_values.TryGetValue(culture.Name, out var cultureValues)
            && cultureValues.TryGetValue(key, out string? value))
        {
            return new LocalizedString(key, value, culture, false, Name);
        }

        return new LocalizedString(key, key, culture, true, Name);
    }
}
```

Provider rules:

- Return `ResourceNotFound = true` when a key is absent instead of throwing for normal misses.
- Preserve empty strings as valid translations.
- Set a stable `ProviderName` so diagnostics can identify the source.
- Throw only for exceptional provider failures such as corrupt data, inaccessible storage, or invalid provider configuration.
- Keep provider lookup independent of UI framework types.

Use `CompositeTranslationProvider` to define deterministic provider order:

```csharp
ITranslationProvider provider = new CompositeTranslationProvider(
    new InMemoryTranslationProvider("Overrides"),
    new ResourceManagerTranslationProvider(Resources.Strings.ResourceManager),
    new JsonTranslationProvider(jsonValues));
```

Provider exceptions are reported as structured diagnostics and either continued or rethrown according to `TranslationProviderFailureBehavior`.

## ResourceManager And IStringLocalizer

`.resx` and satellite assembly users can use `ResourceManagerTranslationProvider`:

```csharp
var provider = new ResourceManagerTranslationProvider(
    Resources.Strings.ResourceManager,
    name: "AppResources");
```

Applications already using Microsoft.Extensions localization can register the `IStringLocalizer` provider from `ProTranslate.MicrosoftExtensions`:

```csharp
services.AddLocalization();
services.AddProTranslateStringLocalizer<AppResources>();
services.AddProTranslate(culture: CultureInfo.GetCultureInfo("en-US"));
```

`StringLocalizerTranslationProvider` temporarily applies the requested culture to `CultureInfo.CurrentCulture` and `CurrentUICulture` while reading the localizer because that is how `IStringLocalizer` resolves culture. Keep that behavior in mind in highly concurrent custom hosting scenarios.

## Region Profiles

Implement `IRegionProfileProvider` when culture-derived `RegionInfo` is not enough. Common cases include tenant policy, user-selected market, or compliance-driven region display.

```csharp
public sealed class TenantRegionProfileProvider : IRegionProfileProvider
{
    private readonly Func<RegionInfo?> _tenantRegion;

    public TenantRegionProfileProvider(Func<RegionInfo?> tenantRegion)
    {
        _tenantRegion = tenantRegion;
    }

    public RegionProfile GetProfile(CultureInfo culture, RegionInfo? regionOverride = null)
    {
        RegionInfo region = regionOverride
            ?? _tenantRegion()
            ?? new RegionInfo(CultureInfo.CreateSpecificCulture(culture.Name).Name);

        return new RegionProfile(region, culture);
    }
}
```

Region profile providers should return immutable `RegionProfile` values and should not mutate global culture state.

## Measurement Systems

Implement `IMeasurementSystemResolver` when the default mapping is not sufficient:

```csharp
public sealed class ProductMeasurementResolver : IMeasurementSystemResolver
{
    public MeasurementSystemProfile Resolve(
        RegionProfile regionProfile,
        MeasurementSystem? measurementSystemOverride = null)
    {
        if (measurementSystemOverride is MeasurementSystem explicitSystem)
        {
            return CreateProfile(explicitSystem);
        }

        return regionProfile.TwoLetterISORegionName == "US"
            ? CreateProfile(MeasurementSystem.USCustomary)
            : CreateProfile(MeasurementSystem.Metric);
    }

    private static MeasurementSystemProfile CreateProfile(MeasurementSystem system) =>
        system == MeasurementSystem.USCustomary
            ? new MeasurementSystemProfile(system, "ft", "F", "lb", "gal", "mph")
            : new MeasurementSystemProfile(MeasurementSystem.Metric, "m", "C", "kg", "l", "km/h");
}
```

Core behavior exposes measurement-system metadata, override APIs, unit conversion, and localized unit formatting. Replace `IUnitConversionService` or `ILocalizedUnitFormatter` when an application needs domain-specific conversion rules, unit symbols, or rounding policy.

## Diagnostic Sinks

Use `IProTranslateDiagnosticSink` or `ITranslationService.DiagnosticReported` to collect missing keys, provider failures, format failures, and region profile failures.

```csharp
public sealed class LoggingDiagnosticSink : IProTranslateDiagnosticSink
{
    private readonly ILogger<LoggingDiagnosticSink> _logger;

    public LoggingDiagnosticSink(ILogger<LoggingDiagnosticSink> logger)
    {
        _logger = logger;
    }

    public void Report(ProTranslateDiagnostic diagnostic)
    {
        _logger.Log(
            diagnostic.Severity == ProTranslateDiagnosticSeverity.Error
                ? LogLevel.Error
                : LogLevel.Warning,
            diagnostic.Exception,
            "{Kind}: {Message}",
            diagnostic.Kind,
            diagnostic.Message);
    }
}
```

Pass a sink to `TranslationService` when constructing services manually:

```csharp
var translations = new TranslationService(
    provider,
    cultures,
    new TranslationFallbackOptions(),
    diagnosticSink);
```

The Microsoft.Extensions helper currently registers default services and does not expose a fluent options builder. Register custom services explicitly when an application needs a custom provider, custom region policy, custom measurement resolver, or custom diagnostic wiring.

## Registration Shape

Manual registration keeps ownership clear:

```csharp
services.AddSingleton<ICultureService>(
    _ => new CultureService(CultureInfo.GetCultureInfo("en-US")));
services.AddSingleton<ITranslationProvider>(_ => new JsonTranslationProvider(jsonValues));
services.AddSingleton<IRegionProfileProvider, TenantRegionProfileProvider>();
services.AddSingleton<IMeasurementSystemResolver, ProductMeasurementResolver>();
services.AddSingleton<IProTranslateDiagnosticSink, LoggingDiagnosticSink>();
services.AddSingleton<ITranslationService>(sp => new TranslationService(
    sp.GetRequiredService<ITranslationProvider>(),
    sp.GetRequiredService<ICultureService>(),
    new TranslationFallbackOptions
    {
        DefaultCulture = CultureInfo.GetCultureInfo("en-US")
    },
    sp.GetRequiredService<IProTranslateDiagnosticSink>()));
services.AddSingleton<IGlobalizationService>(sp => new GlobalizationService(
    sp.GetRequiredService<ICultureService>(),
    sp.GetRequiredService<ITranslationService>(),
    sp.GetRequiredService<IRegionProfileProvider>(),
    sp.GetRequiredService<IMeasurementSystemResolver>()));
```

Then register only the adapter package needed by the application, such as `AddProTranslateAvalonia`, `AddProTranslateWpf`, `AddProTranslateMaui`, `AddProTranslateWinUI`, or `AddProTranslateUno`.

## SOLID Boundaries

- Providers own lookup only.
- `TranslationService` owns fallback, missing-key behavior, formatting failure policy, observable strings, and translation diagnostics.
- `GlobalizationService` owns region and measurement policy composition.
- Adapters own native markup extensions, attached properties, dispatcher/binding refresh, and native flow-direction mapping.
- Source generation owns key constants and accessor code, not runtime policy.

When adding an extension point, prefer a small interface in the core abstractions over a framework-specific service. This keeps the extension reusable across all XAML adapters and keeps `ProTranslate.Core` free of UI dependencies.

## Failure Behavior

Use `TranslationFallbackOptions` to control failure policy:

```csharp
var options = new TranslationFallbackOptions
{
    DefaultCulture = CultureInfo.GetCultureInfo("en-US"),
    ReturnKeyWhenMissing = true,
    ProviderFailureBehavior = TranslationProviderFailureBehavior.ReportAndContinue,
    FormatFailureBehavior = TranslationFormatFailureBehavior.ReportAndReturnUnformatted
};
```

Production applications usually continue after provider failures and display key or fallback text for missing resources. Test and authoring tools may prefer `ReportAndThrow` to fail fast.
