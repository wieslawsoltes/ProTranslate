# Migration Guide

This guide covers adopting ProTranslate in existing XAML applications. There is no previous stable ProTranslate API to migrate from yet, so the focus is moving existing localization paths onto the implemented services.

## Inputs

- existing XAML app using direct resources, `.resx`, JSON resources, or custom localization services
- desired initial culture
- optional `ResourceManager`, `IStringLocalizer`, or custom `ITranslationProvider`
- framework adapter package for Avalonia, WPF, MAUI, WinUI, or Uno

## Outputs

- registered `ICultureService` and `ITranslationService`
- XAML markup-extension usage for translated labels
- culture switching through `ICultureService.SetCulture`
- optional strongly typed key constants and accessor helpers generated from text or JSON catalogs

## Step 1: Choose Provider Strategy

For in-memory or JSON-loaded resources, populate `InMemoryTranslationProvider`:

```csharp
var provider = new InMemoryTranslationProvider()
    .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.FileMenu", "File")
    .Add(CultureInfo.GetCultureInfo("pl-PL"), "Shell.FileMenu", "Plik");
```

For `.resx` resources, use `ResourceManagerTranslationProvider`.

For Microsoft.Extensions localization, use `AddProTranslateStringLocalizer<TResource>()` or construct `StringLocalizerTranslationProvider`.

## Step 2: Register Core Services

```csharp
services.AddProTranslate(
    provider: provider,
    culture: CultureInfo.GetCultureInfo("en-US"));
```

Non-DI hosts can construct services directly:

```csharp
var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
var translations = new TranslationService(provider, cultures);
```

## Step 3: Connect A Framework Adapter

Use the adapter DI helper where the application already uses Microsoft.Extensions:

```csharp
services.AddProTranslateAvalonia();

using ServiceProvider serviceProvider = services.BuildServiceProvider();
serviceProvider.UseProTranslateAvalonia();
```

For direct setup, connect the static adapter binding source:

```csharp
ProTranslate.Avalonia.TranslationService.UseService(translations, cultures);
```

Use the matching adapter setup for WPF, MAUI, WinUI, or Uno. Avalonia and WPF can use ProTranslate from the framework default XML namespace after the adapter assembly is referenced. MAUI should use the shared URI because its default namespace is protected:

```xml
xmlns:pt="https://github.com/protranslate/xaml"
```

WinUI should use:

```xml
xmlns:pt="using:ProTranslate.WinUI"
```

Uno emits a shared URI mapping for Uno tooling that honors `XmlnsDefinition`, but the sample keeps `xmlns:pt="using:ProTranslate.Uno"` for WinUI-compatible XAML.

## Step 4: Replace Direct Resource Lookups In XAML

Before:

```xml
<TextBlock Text="{StaticResource ShellFileMenu}" />
```

After:

```xml
<TextBlock Text="{Translate Shell.FileMenu}" />
```

Apply culture and automatic flow direction at a useful root:

```xml
<Grid Translation.Culture="{Binding Strings.Culture}"
      Translation.AutoFlowDirection="True" />
```

Use the `pt:` form for MAUI, WinUI, and portable snippets that should not depend on a default namespace mapping.

## Step 5: Move Dynamic Text To View Models When Needed

Use `ITranslationService.Format` or a strongly typed view-model proxy for values that combine translation, formatting, and model data:

```csharp
public string InvoiceTotalText => _translations.Format("InvoiceTotalFormat", Total);
```

The sample apps use a strongly typed proxy so compiled bindings and `x:Bind` can reference normal CLR properties.

For simple XAML-owned formatting, use `pt:F`/`pt:Format`:

```xml
<TextBlock Text="{pt:F InvoiceTotalFormat, Value={Binding Total}}" />
```

Avalonia, WPF, and MAUI support bound `Value` formatting through native multi-binding. WinUI and Uno preserve `Value={Binding ...}` through a formatting converter, with view-model or `x:Bind` paths preferred for complex dynamic formatting.

## Step 6: Add Optional Key Constants

Create an additional file named like `Resources.protranslate.keys.txt`:

```text
Shell.FileMenu
Orders.EmptyState
InvoiceTotalFormat
```

The source generator also reads `Strings.*.json` and `*.protranslate.json` catalogs. It emits constants in `ProTranslate.Generated.ProTranslateKeys`, `ProTranslateAccessors.Get_*`, `Value_*`, `Format_*`, and `Observe_*` helper methods, the bindable `ProTranslateStrings` CLR surface, a JSON-backed `ProTranslateGeneratedTranslationProvider`, and provider manifests.

## Constraints

- The source generator emits constants, get/value/format/observe helpers, bindable strings, generated JSON provider code, and provider manifests.
- `ProTranslate.Analyzers` validates missing keys, placeholders, resource coverage, dynamic keys, and invalid catalogs.
- `CultureServiceOptions` controls current/default thread culture mutation.
- WinUI and Uno sample app paths are Windows CI validated.

## Edge Cases

- Missing keys return the key by default and set `LocalizedString.ResourceNotFound`.
- Invalid format strings are reported through structured diagnostics and return the unformatted localized value by default; hosts can opt into throwing.
- Neutral cultures are converted to specific cultures when region metadata is requested.
- Region and measurement-system overrides, unit conversion, and localized unit formatting are reusable core services.

## Validate

- Build the relevant adapter sample.
- Switch cultures at runtime and verify text, formatted strings, and flow direction.
- Check missing-key behavior in tests.
- Run `./build-docs.sh` when documentation changes.
