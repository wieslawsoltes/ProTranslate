---
title: Diagnostics
description: Understand ProTranslate diagnostic kinds, events, sinks, localized result metadata, and failure policies.
---

# Diagnostics

ProTranslate reports lookup and formatting problems as structured runtime diagnostics and ships Roslyn analyzer diagnostics for build-time catalog validation. Runtime diagnostics are framework-neutral and can be consumed from core services, adapters, tests, logging bridges, or host application monitoring.

## Diagnostic Shape

Each diagnostic is a `ProTranslateDiagnostic`:

```csharp
public sealed record ProTranslateDiagnostic(
    ProTranslateDiagnosticKind Kind,
    ProTranslateDiagnosticSeverity Severity,
    string Message,
    string? Key = null,
    CultureInfo? Culture = null,
    string? ProviderName = null,
    Exception? Exception = null);
```

Severities are `Information`, `Warning`, and `Error`.

Implemented diagnostic kinds are:

| Kind | Default severity | When it is used |
| --- | --- | --- |
| `MissingTranslation` | `Warning` | No configured provider or fallback culture resolves a key. |
| `ProviderFailure` | `Error` | A provider throws while resolving a key. |
| `FormatFailure` | `Error` | A localized format string cannot be formatted with the supplied arguments. |
| `RegionProfileFailure` | varies by caller | Reserved in the abstractions for region-resolution failures; the current default region provider does not emit it. |

## Result Metadata

Every lookup returns a `LocalizedString`:

```csharp
LocalizedString value = translations.GetString("Shell.Save");

string text = value.Value;
bool missing = value.ResourceNotFound;
string? provider = value.ProviderName;
IReadOnlyList<ProTranslateDiagnostic> diagnostics = value.Diagnostics;
```

Use `ResourceNotFound` for normal missing-key handling in application code. Use diagnostics for observability, tests, and policy decisions.

When the key is missing after the fallback chain, the returned `LocalizedString` contains:

- `ResourceNotFound == true`
- `ProviderName` set to the top-level provider name
- `Value` equal to the key by default, or an empty string when `ReturnKeyWhenMissing` is `false`
- one `MissingTranslation` diagnostic in `Diagnostics`

Provider diagnostics collected by `CompositeTranslationProvider` can also flow through the result.

## Diagnostic Event

Subscribe to `ITranslationService.DiagnosticReported` to observe runtime diagnostics:

```csharp
translations.DiagnosticReported += (_, args) =>
{
    ProTranslateDiagnostic diagnostic = args.Diagnostic;
    logger.LogWarning(
        "ProTranslate {Kind} for {Key} in {Culture}: {Message}",
        diagnostic.Kind,
        diagnostic.Key,
        diagnostic.Culture?.Name,
        diagnostic.Message);
};
```

`TranslationService` reports diagnostics from three paths:

- diagnostics returned by the provider result
- provider exceptions caught by the service
- missing-key and format-failure diagnostics created by the service

The event is raised after the optional diagnostic sink is called.

## Diagnostic Sink

Use `IProTranslateDiagnosticSink` when diagnostics should be centralized outside event subscriptions.

```csharp
public sealed class LoggingDiagnosticSink : IProTranslateDiagnosticSink
{
    public void Report(ProTranslateDiagnostic diagnostic)
    {
        // Forward to ILogger, OpenTelemetry, test capture, or host diagnostics.
    }
}

var translations = new TranslationService(provider, cultures, options, new LoggingDiagnosticSink());
```

The current core calls the sink synchronously before raising `DiagnosticReported`. Sinks should avoid throwing; the preview implementation does not isolate sink exceptions.

## Missing Translation Policy

Missing keys are not exceptions by default. Configure display behavior with `ReturnKeyWhenMissing`:

```csharp
var options = new TranslationFallbackOptions
{
    ReturnKeyWhenMissing = true
};
```

`true` is useful during development because the missing key remains visible in the UI. `false` is useful when the host wants empty UI text and separate diagnostic reporting.

Adapters also expose `FallbackValue` on markup extensions and attached properties:

```xml
<TextBlock Text="{pt:T Orders.EmptyState, FallbackValue='No orders'}" />
<TextBlock pt:Translation.Key="Orders.EmptyState"
           pt:Translation.FallbackValue="No orders" />
```

`FallbackValue` is adapter display behavior. It does not change the core `LocalizedString` or suppress diagnostics.

## Provider Failure Policy

Provider exceptions are controlled by `TranslationProviderFailureBehavior`:

```csharp
var options = new TranslationFallbackOptions
{
    ProviderFailureBehavior = TranslationProviderFailureBehavior.ReportAndContinue
};
```

`ReportAndContinue` reports `ProviderFailure` and continues fallback lookup. This is the default and is appropriate for production UI paths where a later provider or fallback culture might still resolve the key.

`ReportAndThrow` reports `ProviderFailure` and rethrows the provider exception. Use it in tests, startup validation, or fail-fast hosts.

`CompositeTranslationProvider` has its own provider failure behavior for providers inside the composite. With the default behavior, it records provider failures as result diagnostics and continues to the next provider.

## Format Failure Policy

Formatting failures are controlled by `TranslationFormatFailureBehavior`:

```csharp
var options = new TranslationFallbackOptions
{
    FormatFailureBehavior = TranslationFormatFailureBehavior.ReportAndReturnUnformatted
};
```

`ReportAndReturnUnformatted` reports `FormatFailure` and returns the localized format string unchanged. This is the default.

`ReportAndThrow` reports `FormatFailure` and rethrows the `FormatException`. Use it when placeholder mismatches must stop the workflow.

Typical causes are malformed format strings, placeholder count mismatches, or format specifiers that do not apply to supplied values.

## Analyzer Diagnostics

`ProTranslate.Analyzers` validates static key usage and catalog coverage during compilation:

| Id | Meaning |
| --- | --- |
| `PTA001` | A static key used in code is not present in the configured catalogs. |
| `PTA002` | A `Format` call supplies a different number of arguments than the catalog value requires. |
| `PTA003` | A culture catalog is missing a key that exists in another catalog. |
| `PTA004` | A key expression is dynamic and cannot be checked. |
| `PTA005` | A catalog additional file is invalid. |

Use `.editorconfig` to promote high-confidence diagnostics to errors once catalogs are stable.

## Cache Diagnostics

`TranslationCacheOptions` and `ITranslationCacheInvalidator` expose cache policy and explicit invalidation, but the current runtime does not emit cache hit/miss diagnostics. Add logging around provider implementations when detailed lookup tracing is required.

## Provider-Specific Behavior

`ResourceManagerTranslationProvider` treats `MissingManifestResourceException` and `MissingSatelliteAssemblyException` as missing resources. It returns `ResourceNotFound: true` instead of reporting `ProviderFailure`.

`StringLocalizerTranslationProvider` maps Microsoft.Extensions `LocalizedString.ResourceNotFound` directly into ProTranslate result metadata.

Custom providers decide what is a normal miss and what is a provider failure. Use misses for absent keys and exceptions for broken provider state.

## Preview Boundaries

Diagnostics currently cover runtime missing translations, provider failures, format failures, and build-time analyzer checks. Rich provider trace events, cache hit/miss diagnostics, logging bridges, and debug overlays remain extension points.
