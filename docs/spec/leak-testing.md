# Leak Testing Plan

## Scope

Leak validation covers disposable objects, event subscriptions, static adapter sources, weak target tracking, culture-change refresh paths, and generated binding-safe surfaces.

## Inputs

- active culture and UI culture services
- translation services, globalization services, observable localized strings, and generated string facades
- adapter binding sources and static adapter translation sources
- attached translation keys, fallback values, string formats, and automatic flow-direction properties
- platform UI targets where a stable local harness exists

## Outputs

- release-only leak assertions using `WeakReference`
- adapter-specific tests that exercise source replacement, service replacement, disposal, and target collection
- Avalonia headless window lifecycle coverage with dispatcher, layout, compositor, loaded-queue, and timer cleanup
- dedicated runner and CI workflow that execute the available leak suites per operating system

## Constraints

- `ProTranslate.Core` leak tests stay framework-neutral.
- Avalonia target lifecycle tests use `Avalonia.Headless`.
- Uno `net10.0` tests cover non-visual adapter lifetime paths because direct `Microsoft.UI.Xaml.Controls` construction requires platform bootstrap and fails against the reference assembly in a plain unit-test process.
- WPF and WinUI leak tests are Windows-only.
- MAUI tests run as a portable reflection harness against the built Mac Catalyst adapter assembly, which allows binding-source and MAUI control target checks without launching an app host.
- Leak assertions run only in Release to avoid Debug JIT local-root false positives.

## Edge Cases

- disposed translation services must be released while their culture service stays alive
- disposed globalization services must be released while culture and translation services stay alive
- disposed observable strings and generated string facades must unsubscribe from translation culture changes
- adapter binding sources must release both old and current translation services after `UseService` and `Dispose`
- replaced static adapter sources must be released after `UseSource`
- attached targets must not be rooted by subscriptions, source replacement, culture refresh, or closed windows
- adapter source replacement must not keep stale source subscriptions alive

## Validation

| Area | Validation |
| --- | --- |
| Core | `tests/ProTranslate.Tests/CoreLeakTests.cs` |
| Source generator | generated `ProTranslateStrings` runtime collection test in `tests/ProTranslate.Tests/SourceGeneratorTests.cs` |
| Avalonia | `tests/ProTranslate.Avalonia.Tests/AvaloniaAdapterLeakTests.cs` with headless sessions |
| Uno | `tests/ProTranslate.Uno.Tests` binding-source and static-source leak tests |
| WPF | `tests/ProTranslate.Wpf.Tests` on Windows |
| WinUI | `tests/ProTranslate.WinUI.Tests` on Windows |
| MAUI | `tests/ProTranslate.Maui.Tests` after building `src/ProTranslate.Maui` |
| Runner | `pwsh tools/run-leak-tests.ps1 -Configuration Release` |

