---
title: Translation Providers
description: Configure ProTranslate provider lookup, culture fallback, ResourceManager, IStringLocalizer, and custom providers.
---

# Translation Providers

ProTranslate keeps provider policy in the framework-neutral core. XAML adapters ask `ITranslationService` for a key; they do not own fallback, provider ordering, missing-key policy, or diagnostics.

The current preview ships these provider paths:

- `InMemoryTranslationProvider` for dictionaries loaded by the host.
- `CompositeTranslationProvider` for deterministic provider priority.
- `ResourceManagerTranslationProvider` for `.resx` and satellite assembly resources.
- `StringLocalizerTranslationProvider` for Microsoft.Extensions localization.
- Any custom provider that implements `ITranslationProvider`.

Import/export formats such as XLIFF, PO/POT, RESX, Android `strings.xml`, Apple string files, Flutter ARB, i18next JSON, and CSV/TSV are tooling concerns. They should be converted into normalized ProTranslate catalogs and, for app runtime, into source-generated provider code or explicit provider registrations. Providers should not silently perform lossy format conversion during lookup.

## Provider Contract

Providers implement a small synchronous contract:

```csharp
public interface ITranslationProvider
{
    string Name { get; }

    LocalizedString GetString(string key, CultureInfo culture);
}
```

Return a `LocalizedString` for every lookup. Missing keys should normally return `ResourceNotFound: true` instead of throwing.

```csharp
public sealed class JsonTranslationProvider : ITranslationProvider
{
    public string Name => "Json";

    public LocalizedString GetString(string key, CultureInfo culture)
    {
        if (TryGetValue(culture.Name, key, out string? value))
        {
            return new LocalizedString(key, value, culture, false, Name);
        }

        return new LocalizedString(key, key, culture, true, Name);
    }
}
```

Empty strings are valid translations. `TranslationService` treats `Value == ""` with `ResourceNotFound == false` as an intentional result and does not report a missing-key diagnostic.

## Lookup Pipeline

`TranslationService.GetString` owns culture fallback. For the requested UI culture it tries:

1. The requested culture.
2. Parent cultures, when `UseParentCultures` is `true`.
3. Each culture in `TranslationFallbackOptions.FallbackCultures`, including parents when enabled.
4. `TranslationFallbackOptions.DefaultCulture`, including parents when enabled.

The defaults are:

```csharp
var options = new TranslationFallbackOptions
{
    DefaultCulture = CultureInfo.InvariantCulture,
    UseParentCultures = true,
    UseDefaultCulture = true,
    ReturnKeyWhenMissing = true,
    ProviderFailureBehavior = TranslationProviderFailureBehavior.ReportAndContinue,
    FormatFailureBehavior = TranslationFormatFailureBehavior.ReportAndReturnUnformatted
};
```

When no provider returns a value, the service reports a `MissingTranslation` diagnostic. By default the returned value is the key. Set `ReturnKeyWhenMissing = false` to return an empty string instead.

```csharp
var options = new TranslationFallbackOptions
{
    ReturnKeyWhenMissing = false
};

options.FallbackCultures.Add(CultureInfo.GetCultureInfo("en-US"));
```

## Provider Priority

Use `CompositeTranslationProvider` when multiple sources are active. Providers are tried in constructor order for each culture candidate.

```csharp
var tenant = new InMemoryTranslationProvider("Tenant");
var product = new ResourceManagerTranslationProvider(Resources.Strings.ResourceManager, "ProductResx");
var defaults = new InMemoryTranslationProvider("Defaults");

var provider = new CompositeTranslationProvider(tenant, product, defaults);
var service = new TranslationService(provider, cultures);
```

For `fr-CA`, the effective order is:

1. `Tenant` for `fr-CA`, then `ProductResx` for `fr-CA`, then `Defaults` for `fr-CA`.
2. `Tenant` for `fr`, then `ProductResx` for `fr`, then `Defaults` for `fr`.
3. Explicit fallback cultures and the default culture, using the same provider order.

This makes provider priority predictable while keeping culture fallback centralized in `TranslationService`.

## ResourceManager

Use `ResourceManagerTranslationProvider` when translations live in `.resx` resources.

```csharp
var provider = new ResourceManagerTranslationProvider(
    Resources.Strings.ResourceManager,
    "AppResources");

var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
var translations = new TranslationService(provider, cultures);
```

The provider calls `ResourceManager.GetString(key, culture)`. If the resource manager returns a value, ProTranslate returns that value with the requested culture and provider name. If the resource is missing, or the manifest or satellite assembly is unavailable, the provider returns a missing `LocalizedString`. The current provider treats missing manifests and missing satellite assemblies as resource misses rather than provider failures.

`ResourceManager` already has its own resource probing behavior. ProTranslate still applies its explicit fallback chain around provider calls, so tests can verify fallback order consistently across provider types.

## IStringLocalizer

Use `StringLocalizerTranslationProvider` when the application already uses Microsoft.Extensions localization.

```csharp
services.AddLocalization();
services.AddProTranslateStringLocalizer<SharedResource>();
services.AddProTranslate(culture: CultureInfo.GetCultureInfo("en-US"));
```

You can also construct the provider directly:

```csharp
var provider = new StringLocalizerTranslationProvider(localizer, "SharedLocalizer");
```

The provider reads `_localizer[key]` and maps the Microsoft.Extensions `ResourceNotFound` flag into `LocalizedString.ResourceNotFound`. Because `IStringLocalizer` resolves against ambient .NET culture state, the current implementation temporarily sets `CultureInfo.CurrentCulture` and `CultureInfo.CurrentUICulture` to the lookup culture, then restores the previous values.

## Custom Providers

Custom providers should keep lookup policy narrow:

- Do return `ResourceNotFound: true` for normal misses.
- Do set a stable, useful `Name` for diagnostics.
- Do preserve intentional empty strings.
- Do throw only for real provider failures such as unreadable files, invalid external state, or network errors.
- Do not duplicate parent-culture or default-culture fallback inside the provider unless the external system requires it.

Provider failures are reported as structured diagnostics by `TranslationService`. With the default `ReportAndContinue` policy, lookup continues through the fallback chain. With `ReportAndThrow`, the original provider exception is rethrown after the diagnostic is reported.

## Cache And Manifest Support

`TranslationCacheOptions` controls translation lookup caching in `TranslationService`, and `ITranslationCacheInvalidator` lets hosts clear or remove cached values when a provider reloads. `ProTranslate.SourceGenerator` emits `ProTranslateProviderManifest` for catalog metadata discovered at build time and `ProTranslateGeneratedTranslationProvider` when JSON catalog values should be compiled into provider code.

The preview provider pipeline is synchronous. Rich provider traces, cache hit/miss diagnostics, and logging/debug-overlay integrations remain extension points.

For professional localization handoff, prefer XLIFF for CAT/TMS exchange and source-generated ProTranslate catalogs for runtime lookup. Format conversion should report loss-aware diagnostics for unsupported constructs.
