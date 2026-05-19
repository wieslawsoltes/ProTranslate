# ProTranslate Validation Matrix

## Current Validation Status

| Area | Current check | Environment | Status |
| --- | --- | --- | --- |
| Portable libraries | `dotnet build ProTranslate.CI.slnx -c Release` | macOS, Linux, Windows CI | Implemented |
| Core tests | `tests/ProTranslate.Tests` | macOS, Linux, Windows CI | Implemented |
| Analyzer tests | `tests/ProTranslate.Analyzers.Tests` | macOS, Linux, Windows CI | Implemented |
| Avalonia adapter smoke/runtime/leak tests | `tests/ProTranslate.Avalonia.Tests` | macOS, Linux, Windows CI | Implemented |
| Docs site | `./build-docs.sh` | docs workflow and local validation | Implemented |
| Avalonia sample | `samples/ProTranslate.Avalonia.Sample` build | macOS local/CI path | Implemented |
| MAUI adapter and sample restore | `src/ProTranslate.Maui` build and `samples/ProTranslate.Maui.Sample` restore | macOS CI with MAUI workload | Implemented; full sample app packaging remains local/platform validation |
| WPF sample | `samples/ProTranslate.Wpf.Sample` build | Windows CI | Implemented |
| WinUI sample | `samples/ProTranslate.WinUI.Sample` build | Windows CI | Implemented |
| Uno sample | `samples/ProTranslate.Uno.Sample` build | Windows CI | Implemented |
| Package artifacts | `./pack.sh <version>` | release workflow and local validation | Implemented for portable packages; MAUI package requires MAUI workload |
| Translation format tooling | Parser/writer, round-trip, loss diagnostics, placeholder/plural mapping, and source-generator compile tests | Future tooling CI | Initial surface exists; validation hardening planned |
| Uno Translation Studio | Build and UI smoke tests for import preview, editing, diagnostics, and export preview | Future Uno app CI | Initial sample exists; validation hardening planned |

Not yet validated by automated tests:
- full round-trip validation for XLIFF, PO/POT, RESX, Android, Apple, ARB, i18next JSON, and CSV/TSV
- professional Uno Translation Studio workflow UI smoke coverage
- adapter memory retention and weak target behavior outside current Avalonia coverage
- provider trace/cache diagnostics and logging/debug-overlay integrations
- platform runtime UI automation for WPF, MAUI, WinUI, and Uno

## Culture Matrix

| Culture | Region | Direction | Measurement Default | Must Validate |
| --- | --- | --- | --- | --- |
| `en-US` | US | LTR | US customary | currency, short date, decimal separator, region name, inches/miles/pounds |
| `en-GB` | GB | LTR | imperial-compatible profile | currency, date order, metric/imperial preference policy |
| `pl-PL` | PL | LTR | metric | diacritics, currency, decimal separator, metric units |
| `fr-CA` | CA | LTR | metric | language/region combination, currency, date and number formats |
| `ar-SA` | SA | RTL | metric profile unless overridden | `FlowDirection`, calendar behavior, numeric formatting |
| `he-IL` | IL | RTL | metric | `FlowDirection`, bidi text, fallback behavior |
| `ja-JP` | JP | LTR | metric | non-Latin scripts, calendar/profile formatting |

## Provider Validation

Inputs:
- same keys in `ResourceManager` and `IStringLocalizer`
- missing child-culture resources
- duplicate keys across providers
- invalid format strings

Outputs:
- deterministic translation result
- fallback path
- current missing-resource metadata and structured diagnostics

Constraints:
- missing key is not a production crash by default
- provider exception policies are configurable
- empty string can be intentional

Edge cases:
- parent culture fallback
- invariant culture fallback
- provider timeout or exception
- placeholder mismatch

Validate:
- priority order tests
- fallback tests
- missing-key `LocalizedString.ResourceNotFound` state
- provider exception policy tests
- format failure diagnostics

## Culture Switching Validation

Inputs:
- active view with translated strings
- active view with formatted values
- active view with automatic flow direction
- pending binding updates

Outputs:
- refreshed translated values
- refreshed formatted values
- updated native `FlowDirection`
- no retained detached targets for current Avalonia leak-test paths

Constraints:
- current validation covers binding-source refresh and Avalonia runtime/leak paths; broader dispatcher guarantees are planned
- culture changes are observable through service events
- adapter target tracking and leak assertions remain planned outside current Avalonia coverage

Edge cases:
- culture switch during page navigation
- switch to same culture
- switch while provider refresh is pending
- target disposed before update arrives

Validate:
- Avalonia repeated culture switch and formatted refresh tests
- Avalonia detached target garbage collection tests
- planned dispatcher marshalling tests for non-Avalonia adapters
- same-culture idempotence test

## XAML Adapter Validation

| Adapter | Required Checks |
| --- | --- |
| Avalonia | `T`/`Translate` and `F`/`Format` binding creation, prefix-free default namespace sample syntax, binding-source refresh, attached key/fallback/string-format behavior, sample build, culture and flow-direction runtime tests, release-only leak tests |
| WPF | sample build on Windows CI, prefix-free default namespace sample syntax, dependency-property key/culture/flow-direction path in sample; UI automation planned |
| MAUI | adapter build and sample restore on macOS CI, local Mac Catalyst sample build, shared ProTranslate URI syntax, bindable key/culture/flow-direction path in sample; mobile lifecycle tests planned |
| WinUI | sample build on Windows CI, `x:Bind` view-model path and attached `Translation.Key` sample path; UI automation planned |
| Uno | sample build on Windows CI, WinUI-compatible `x:Bind`, attached `Translation.Key`, and explicit namespace syntax; Skia/WebAssembly runtime validation planned |

## Compiled Binding And `x:Bind`

Inputs:
- generated typed key constants, accessors, provider entries, and `ProTranslateStrings` properties
- XAML compiled binding paths
- renamed or removed resource keys

Outputs:
- compile success for valid keys
- analyzer diagnostics for invalid keys
- deterministic generated names
- generated CLR properties consumable by Avalonia compiled binding and WinUI/Uno `x:Bind`

Constraints:
- generated code is framework-neutral
- adapter-specific samples compile independently
- no reflection-only path for normal static keys

Edge cases:
- duplicate normalized identifiers
- punctuation in keys
- culture file mismatch
- stale generated file

Validate:
- source generator snapshot tests
- JSON catalog tests
- generated accessor compile tests
- duplicate/invalid catalog diagnostic tests
- analyzer diagnostics tests
- Avalonia compiled binding sample build
- WinUI/Uno `x:Bind` sample builds on Windows CI

## Translation Format Import And Export

Inputs:
- XLIFF 1.2 and 2.1
- gettext PO and POT
- .NET RESX
- Android `strings.xml`
- Apple `.strings`, `.stringsdict`, and `.xcstrings`
- Flutter ARB
- i18next JSON
- CSV and TSV
- normalized ProTranslate catalogs

Outputs:
- imported normalized catalog records
- source-generator-compatible JSON catalogs
- key-only catalogs
- exported exchange files
- loss-aware diagnostics for unsupported constructs

Constraints:
- XLIFF is the preferred CAT/TMS exchange format
- source-generated ProTranslate catalogs are the preferred app runtime format
- unsupported constructs must produce diagnostics
- parser/writer behavior must be deterministic
- runtime providers and adapters must not silently parse exchange formats

Edge cases:
- unsupported XLIFF modules or inline code patterns
- gettext plural formulas and obsolete or fuzzy entries
- non-string RESX resources
- Android styled strings and quantity rules
- Apple variation metadata and stringsdict plural dictionaries
- ARB ICU plurals and selects
- i18next namespaces, plural suffixes, and interpolation
- CSV/TSV delimiter, encoding, formula, and duplicate-key problems

Validate before claiming support:
- parser and writer unit tests per format
- import/export round-trip fixtures per format
- explicit lossy-conversion diagnostics
- placeholder mapping tests across .NET, ICU, printf, XLIFF, Android, and i18next syntaxes
- plural mapping tests across gettext, Android, Apple, ARB, and i18next
- culture tag normalization tests
- importer output compiles through `ProTranslate.SourceGenerator`
- analyzers report missing keys, placeholder mismatches, coverage gaps, and invalid imported catalogs
- docs build

## Uno Translation Studio

Inputs:
- normalized ProTranslate catalogs
- imported external files
- source-generator manifests
- source and target cultures
- translator edits, review state, diagnostics filters, and export commands

Outputs:
- edited normalized catalogs
- source-generator-ready catalogs
- export files
- diagnostics report
- unresolved issue list

Constraints:
- authoring UI remains separate from `ProTranslate.Uno` adapter runtime
- shared import/export services stay framework-neutral
- `x:Bind` usage should rely on generated `ProTranslateStrings` or typed view-model properties
- validation claims must name the exact Uno target heads that were built and smoke-tested

Edge cases:
- large catalogs
- right-to-left target cultures
- placeholder edits that break formatting
- concurrent file reload and edits
- export target cannot represent required metadata

Validate before claiming support:
- Uno app build for documented target heads
- UI smoke tests for import preview, grid editing, diagnostics filtering, placeholder validation, save, and export preview
- memory and responsiveness checks for large catalogs
- source-generator compile check after saving catalogs

## Region And Measurement

Inputs:
- `CultureInfo`
- `RegionInfo`
- optional user override
- value requiring unit display

Outputs:
- implemented region profile
- current measurement system enum and profile mapping
- localized unit-formatted string
- current preview exposes `RegionProfile`, `IsMetric`, `IGlobalizationService.MeasurementSystem`, `MeasurementSystemProfile`, explicit region/measurement overrides, unit conversion, and localized unit formatting; sample view models use the core conversion services

Constraints:
- user preference wins over region default
- neutral cultures require fallback policy
- default unit conversion rules are explicit and replaceable

Edge cases:
- neutral culture
- unknown region
- mixed unit profile
- culture switch with persisted user override

Validate:
- known-region defaults
- sample override behavior
- core region and measurement override tests
- planned invalid region diagnostics
- implemented unit conversion and localized formatter tests
- planned broader unit format snapshots

## Performance And Reliability

Inputs:
- high-volume lookup path
- frequent culture switches
- many UI targets
- provider cache enabled and disabled

Outputs:
- planned bounded allocation checks
- planned stable lookup latency checks
- planned no-retained-UI-target checks
- explicit cache invalidation results
- planned cache hit/miss diagnostics

Constraints:
- cache policy is implemented; slow-provider isolation checks remain planned
- current lookup path is synchronous

Edge cases:
- slow async provider
- cache stampede after culture switch
- thousands of translated targets
- diagnostic sink failure

Validate:
- lookup benchmarks
- culture switch stress tests
- Avalonia memory leak tests
- planned diagnostic sink isolation tests
