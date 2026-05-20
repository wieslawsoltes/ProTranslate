---
title: Analyzers
description: Build-time ProTranslate diagnostics for catalog and static key correctness.
---

# Analyzers

`ProTranslate.Analyzers` validates static ProTranslate usage during compilation. It is shipped as a Roslyn analyzer package and inspects catalog additional files plus C# calls into the translation services and adapter helper APIs.

## Installation

```xml
<PackageReference Include="ProTranslate.Analyzers"
                  Version="0.1.0-preview"
                  PrivateAssets="all"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

The analyzer reads the same catalog file shapes as the source generator:

```xml
<AdditionalFiles Include="Resources/Strings.*.json" />
<AdditionalFiles Include="Resources/*.protranslate.json" />
<AdditionalFiles Include="Resources/*.protranslate.keys.txt" />
```

## Diagnostics

| Id | Meaning |
| --- | --- |
| `PTA001` | A static translation key is not present in the configured catalogs. |
| `PTA002` | A `Format` call passes a different number of arguments than the catalog value requires. |
| `PTA003` | A culture-specific catalog is missing a key that exists in another catalog. |
| `PTA004` | A key argument is dynamic and cannot be validated at compile time. |
| `PTA005` | A catalog additional file is invalid or unsupported. |

## Validated Usage Shapes

The analyzer recognizes:

- `ITranslationService.GetString`, `Format`, `Observe`, and indexer usage.
- `IGlobalizationService.GetString`.
- Generated `ProTranslate.Generated.ProTranslateAccessors` methods.
- Adapter static helpers such as `ProTranslate.Avalonia.TranslationService.T`.
- Adapter `TranslationBindingSource.Translate` calls.

Dynamic keys are useful for rare provider-driven scenarios, but they cannot be checked against catalog files. Prefer generated constants, `ProTranslateStrings`, or `ProTranslateAccessors` for application UI keys.

## Placeholder Rules

Placeholder analysis uses composite-format indexes such as `{0}`, `{1:N2}`, and `{0,10}`. Escaped braces are ignored. When all catalog values for a key agree on the required argument count, `PTA002` reports mismatched static `Format` calls.

If different cultures intentionally use different placeholder counts, the analyzer does not guess which count is correct. Keep placeholder arity consistent across cultures so every localized value can be formatted by the same view-model code.

## Coverage Rules

`PTA003` compares culture-specific JSON catalogs. It reports when a catalog such as `Strings.pl-PL.json` omits a key that exists in another ProTranslate catalog. Use this for release blocking in applications that require complete localization coverage.

## CI Guidance

Treat analyzer warnings as build warnings by default while migrating an existing application. New ProTranslate applications can promote `PTA001`, `PTA002`, and `PTA003` to errors in `.editorconfig` once catalogs are stable.

```ini
dotnet_diagnostic.PTA001.severity = error
dotnet_diagnostic.PTA002.severity = error
dotnet_diagnostic.PTA003.severity = error
```
