---
title: Performance and Trimming
description: Performance guidance, trimming considerations, culture-switch costs, and current caveats.
---

# Performance and Trimming

ProTranslate favors deterministic synchronous lookup, explicit services, and generated key paths. That makes it straightforward to reason about in desktop and mobile XAML applications, but teams should still design for culture-switch cost, retained UI targets, and trimmed builds.

This page documents implemented behavior and the current caveats. Cache policy APIs, Avalonia leak coverage, and thread-culture opt-out are implemented; lookup benchmarks and broader non-Avalonia runtime automation remain hardening work.

## Hot Paths

The common runtime path is:

1. XAML adapter, view model, or generated accessor requests a key.
2. `TranslationService` enumerates the active UI culture, parent cultures, configured fallback cultures, and default culture.
3. The service checks the configured translation cache policy.
4. The configured provider resolves the key on a cache miss.
5. Missing keys, provider failures, and format failures report structured diagnostics.
6. `Format` uses `string.Format(CurrentCulture, value, arguments)`.

Keep provider lookup fast and predictable. `InMemoryTranslationProvider` uses concurrent dictionaries. `ResourceManagerTranslationProvider` delegates to .NET `ResourceManager`. `StringLocalizerTranslationProvider` delegates to `IStringLocalizer`.

For custom providers:

- Preload or index catalogs before they are used by UI bindings.
- Avoid blocking network or disk I/O during `GetString`.
- Return a missing `LocalizedString` for normal misses instead of throwing.
- Use `CompositeTranslationProvider` for ordered fallback instead of adding fallback logic to adapters.
- Keep diagnostic allocation proportional to actual failures.

## Culture Switching Cost

Culture switching raises events through `ICultureService`, `ITranslationService`, observable strings, globalization services, and adapter binding sources. Every active translated target may refresh.

Practical guidance:

- Switch culture from a single application service or command path.
- Avoid repeated same-culture assignments; `CultureService` treats same culture/UI culture switches as idempotent.
- Batch view-model property notifications when many computed localized properties depend on one switch.
- Dispose `IObservableLocalizedString` instances owned by long-lived view models.
- Avoid thousands of independent observable strings when one typed proxy can raise a coherent set of property changes.

The validation matrix tracks broader non-Avalonia culture-switch stress tests and retained-target assertions as hardening work.

## Adapter Target Tracking

Adapters subscribe XAML targets to binding-source changes for attached translation properties. Current implementations use weak references for attached target subscriptions and dispose subscriptions when keys are cleared or when weak targets disappear during refresh.

Memory-retention guidance:

- Prefer normal bindings or generated view-model properties for large virtualized item collections.
- Clear `Translation.Key` when reusing controls manually outside normal framework lifecycles.
- Keep adapter-specific services at the application layer, not in shared view-model objects.
- Include navigation and repeated culture-switch scenarios in application-level testing.

Avalonia release builds include adapter memory-retention tests for disposed binding sources, observable localized strings, and attached translation targets. Full runtime retention coverage is not yet implemented for WPF, MAUI, WinUI, and Uno.

## Trimming And NativeAOT

Generated constants, accessors, provider entries, and `ProTranslateStrings` properties are trimming-friendly because they are normal statically referenced C# members:

```csharp
using ProTranslate.Generated;

string title = translations.Value_ShellTitle();
string total = translations.Format_OrdersTotal(order.Total);
```

For compiled binding and `x:Bind`, prefer the generated `ProTranslateStrings` CLR surface:

```csharp
public ProTranslateStrings Strings { get; } = new(translations);
```

```xml
<TextBlock Text="{x:Bind ViewModel.Strings.AppTitle, Mode=OneWay}" />
```

This gives XAML compilers a strongly typed member path and avoids depending on stringly typed reflection or resource discovery for normal static keys. Use `ProTranslateGeneratedTranslationProvider` when JSON catalogs should be compiled into provider code instead of loaded through assembly resource enumeration.

Trimming considerations by provider:

| Provider | Trimming notes |
| --- | --- |
| `InMemoryTranslationProvider` | Data is supplied explicitly by the app. Keep catalog loading code trim-safe. |
| `ProTranslateGeneratedTranslationProvider` | JSON catalog values are emitted as generated code, so no runtime resource discovery is needed. |
| `ResourceManagerTranslationProvider` | Ensure `.resx` resources and satellite assemblies are preserved by the project and publish pipeline. |
| `StringLocalizerTranslationProvider` | Follow Microsoft.Extensions.Localization trimming guidance for the host application and resource marker types. |
| Custom providers | Avoid reflection-based discovery unless you add the required trimming annotations or descriptors. |

The source generator itself targets `netstandard2.0` as a Roslyn component and runs at build time. Generated code does not add runtime reflection requirements.

## AdditionalFiles And Build Determinism

Source generation uses `AdditionalFiles`; include only catalogs that should affect generated keys. Generated output is sorted by key and deterministic.

Recommended practices:

- Use `Strings.*.json` for culture catalogs consumed by both runtime loaders and the generator.
- Use `*.protranslate.keys.txt` for key manifests that are independent of a concrete culture.
- Treat `PTSG001` and `PTSG002` warnings as build issues in CI.
- Keep duplicate keys inside one file out of catalogs; duplicates across culture files are expected.
- Avoid generated member name churn by using stable key names.

The generator validates catalog shape and duplicate keys within a file. `ProTranslate.Analyzers` validates missing static keys, placeholder mismatches, resource coverage, dynamic keys, and invalid catalogs in consuming projects.

## Formatting

`ITranslationService.Format` formats with the active formatting culture:

```csharp
string text = translations.Format("Orders.Total", total);
```

Invalid format strings report `ProTranslateDiagnosticKind.FormatFailure`. By default the service returns the unformatted localized value; with `TranslationFormatFailureBehavior.ReportAndThrow`, it rethrows the `FormatException` after reporting the diagnostic.

Performance guidance:

- Keep high-frequency numeric formatting in view models or reusable formatting services.
- Avoid formatting every frame or during layout-only changes.
- Use generated accessors for static keys so refactors remain compile-visible.
- Cover placeholder counts and argument types with unit tests and enable `ProTranslate.Analyzers` for build-time placeholder diagnostics.

## Thread Culture Policy

`CultureService` can apply active cultures to `CultureInfo.DefaultThreadCurrentCulture`, `CultureInfo.DefaultThreadCurrentUICulture`, `Thread.CurrentThread.CurrentCulture`, and `Thread.CurrentThread.CurrentUICulture`. That is the default because many .NET and UI APIs read ambient culture.

Applications with strict isolation requirements can opt out:

```csharp
var options = new CultureServiceOptions
{
    ApplyToCurrentThread = false,
    ApplyToDefaultThread = false
};
```

`StringLocalizerTranslationProvider` also temporarily sets current thread culture while it queries `IStringLocalizer`.

## Diagnostics Cost

Diagnostics are structured values and are part of normal reliability behavior:

- Missing translations report `MissingTranslation`.
- Provider exceptions report `ProviderFailure`.
- Invalid localized format strings report `FormatFailure`.
- Region profile failures are represented in the abstraction model.

Diagnostic sinks should be fast and isolated. Avoid blocking I/O in `IProTranslateDiagnosticSink.Report`; queue to logging infrastructure when necessary. If a diagnostic sink is part of a production path, test sink failure behavior in the host application because the current sink call is direct.

## Validation Checklist

For performance-sensitive or trimmed applications, validate:

- `dotnet publish` with the same trimming/AOT settings used in release.
- Culture switch from `en-US` to an RTL culture such as `ar-SA`.
- Region and measurement overrides after culture switching.
- Missing-key, provider-failure, and format-failure diagnostics.
- Compiled binding or `x:Bind` paths that consume generated accessors.
- Navigation away from translated views followed by garbage collection in application-level leak tests.
- ResourceManager satellite assembly inclusion for every target platform.

Use `./build.sh` for portable validation and `./build-docs.sh` after documentation changes.
