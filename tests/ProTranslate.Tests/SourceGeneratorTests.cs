using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ProTranslate;
using ProTranslate.SourceGenerator;

namespace ProTranslate.Tests;

public sealed class SourceGeneratorTests
{
    [Fact]
    public void KeyGeneratorDisambiguatesIdentifierCollisions()
    {
        MethodInfo render = typeof(ProTranslateKeysGenerator).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Render method was not found.");

        var keys = ImmutableArray.Create("Shell.Title", "Shell-Title");
        var code = (string?)render.Invoke(null, [keys]);

        Assert.NotNull(code);
        Assert.Contains("ShellTitle_", code, StringComparison.Ordinal);
        Assert.Contains(" = \"Shell.Title\"", code, StringComparison.Ordinal);
        Assert.Contains(" = \"Shell-Title\"", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ShellTitle2", code, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyGeneratorExtractsKeysFromJsonCatalogs()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/Strings.en-US.json",
                """
                {
                  "Shell": {
                    "Title": "ProTranslate",
                    "FileMenu": "File"
                  },
                  "Orders.EmptyState": "No orders"
                }
                """));

        Assert.Empty(result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        string generated = GetGeneratedSource(result, "ProTranslateKeys.g.cs");
        Assert.Contains("public const string OrdersEmptyState = \"Orders.EmptyState\";", generated, StringComparison.Ordinal);
        Assert.Contains("public const string ShellFileMenu = \"Shell.FileMenu\";", generated, StringComparison.Ordinal);
        Assert.Contains("public const string ShellTitle = \"Shell.Title\";", generated, StringComparison.Ordinal);
        Assert.Contains("public static ProTranslate.IObservableLocalizedString Observe_ShellTitle", generated, StringComparison.Ordinal);

        string strings = GetGeneratedSource(result, "ProTranslateStrings.g.cs");
        Assert.Contains("public sealed partial class ProTranslateStrings", strings, StringComparison.Ordinal);
        Assert.Contains("public string ShellTitle =>", strings, StringComparison.Ordinal);
        Assert.Contains("OnPropertyChanged(nameof(ShellTitle));", strings, StringComparison.Ordinal);

        string provider = GetGeneratedSource(result, "ProTranslateGeneratedTranslationProvider.g.cs");
        Assert.Contains("public sealed partial class ProTranslateGeneratedTranslationProvider", provider, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateGeneratedTranslationEntry(\"Shell.Title\", \"en-US\", \"ProTranslate\")", provider, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedJsonCatalogHelpersCompile()
    {
        var source = SourceText.From(
            """
            using System.Globalization;
            using ProTranslate;
            using ProTranslate.Generated;

            namespace Demo;

            public static class GeneratedUsage
            {
                public static string Read(ITranslationService translations)
                {
                    _ = ProTranslateKeys.ShellTitle;
                    _ = ProTranslateProviderManifest.Keys.Length;
                    _ = ProTranslateProviderManifest.Cultures.Length;
                    _ = ProTranslateProviderManifest.SourceFiles.Length;
                    _ = ProTranslateProviderManifest.Entries[0].PlaceholderIndexes.Length;
                    _ = new ProTranslateGeneratedTranslationProvider().GetString(ProTranslateKeys.ShellTitle, CultureInfo.GetCultureInfo("en-US")).Value;
                    using var strings = new ProTranslateStrings(translations);
                    _ = strings.ShellTitle;
                    _ = strings.Format_OrdersTotal(12.5m);
                    strings.Refresh();
                    _ = translations.Get_ShellTitle();
                    _ = translations.Observe_ShellTitle();
                    return translations.Value_ShellTitle() + translations.Format_OrdersTotal(12.5m);
                }
            }
            """,
            Encoding.UTF8);

        CSharpCompilation compilation = CreateCompilation(source);
        GeneratorDriver driver = CreateDriver(
            new InMemoryAdditionalText(
                "/translations/App.protranslate.json",
                """
                {
                  "Shell.Title": "ProTranslate",
                  "Orders.Total": "Total: {0:C}"
                }
                """));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> generatorDiagnostics);

        Assert.Empty(generatorDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(outputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    [ReleaseFact]
    public void GeneratedStringsDisposeReleasesCultureSubscriptionLeakTest()
    {
        CSharpCompilation compilation = CreateCompilation(SourceText.From("namespace Demo { public sealed class App { } }", Encoding.UTF8));
        GeneratorDriver driver = CreateDriver(
            new InMemoryAdditionalText(
                "/translations/App.protranslate.json",
                """
                {
                  "Shell.Title": "ProTranslate",
                  "Orders.Total": "Total: {0:C}"
                }
                """));

        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> generatorDiagnostics);

        Assert.Empty(generatorDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        using var peStream = new MemoryStream();
        var emitResult = outputCompilation.Emit(peStream);
        Assert.True(
            emitResult.Success,
            string.Join(Environment.NewLine, emitResult.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));

        Assembly assembly = Assembly.Load(peStream.ToArray());
        Type stringsType = assembly.GetType("ProTranslate.Generated.ProTranslateStrings", throwOnError: true)!;
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var translations = new TranslationService(
            new InMemoryTranslationProvider()
                .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Title", "ProTranslate")
                .Add(CultureInfo.GetCultureInfo("pl-PL"), "Shell.Title", "ProTranslate PL"),
            cultures);

        WeakReference weak = CreateDisposedGeneratedStrings(stringsType, translations, cultures);

        LeakTestHelpers.AssertCollected(weak);
        GC.KeepAlive(translations);
        GC.KeepAlive(cultures);
    }

    [Fact]
    public void KeyGeneratorExtractsKeysFromIndustryStandardFormats()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/Resources.pl-PL.resx",
                """
                <root>
                  <data name="Resx.Title"><value>Tytuł</value></data>
                </root>
                """),
            new InMemoryAdditionalText(
                "/translations/values-pl-rPL/strings.xml",
                """
                <resources>
                  <string name="Android_Title">Tytuł</string>
                  <plurals name="Android_Count">
                    <item quantity="one">{0} element</item>
                    <item quantity="other">{0} elementów</item>
                  </plurals>
                </resources>
                """),
            new InMemoryAdditionalText(
                "/translations/messages.pl-PL.po",
                """
                msgid ""
                msgstr ""
                "Language: pl-PL\n"

                msgid "Po.Title"
                msgstr "Tytuł"
                """),
            new InMemoryAdditionalText(
                "/translations/app.pl-PL.xlf",
                """
                <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
                  <file source-language="en-US" target-language="pl-PL" datatype="plaintext" original="app">
                    <body>
                      <trans-unit id="Xliff.Title" resname="Xliff.Title">
                        <source>Title</source>
                        <target>Tytuł</target>
                      </trans-unit>
                    </body>
                  </file>
                </xliff>
                """),
            new InMemoryAdditionalText(
                "/translations/catalog.csv",
                """
                key,culture,value
                Csv.Title,pl-PL,Tytuł
                """),
            new InMemoryAdditionalText(
                "/translations/pl.lproj/Localizable.strings",
                """
                "Apple.Title" = "Tytuł";
                """),
            new InMemoryAdditionalText(
                "/translations/app.arb",
                """
                {
                  "@@locale": "pl-PL",
                  "ArbTitle": "Tytuł",
                  "@ArbTitle": { "description": "Title" }
                }
                """));

        Assert.Empty(result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        string generated = GetGeneratedSource(result, "ProTranslateKeys.g.cs");
        Assert.Contains("public const string AndroidTitle = \"Android_Title\";", generated, StringComparison.Ordinal);
        Assert.Contains("public const string AndroidCountOne = \"Android_Count.one\";", generated, StringComparison.Ordinal);
        Assert.Contains("public const string AppleTitle = \"Apple.Title\";", generated, StringComparison.Ordinal);
        Assert.Contains("public const string ArbTitle = \"ArbTitle\";", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("@ArbTitle", generated, StringComparison.Ordinal);
        Assert.Contains("public const string CsvTitle = \"Csv.Title\";", generated, StringComparison.Ordinal);
        Assert.Contains("public const string PoTitle = \"Po.Title\";", generated, StringComparison.Ordinal);
        Assert.Contains("public const string ResxTitle = \"Resx.Title\";", generated, StringComparison.Ordinal);
        Assert.Contains("public const string XliffTitle = \"Xliff.Title\";", generated, StringComparison.Ordinal);

        string manifest = GetGeneratedSource(result, "ProTranslateProviderManifest.g.cs");
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Android_Count.one\", \"pl-PL\"", manifest, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateProviderManifestEntry(\"ArbTitle\", \"pl-PL\"", manifest, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Csv.Title\", \"pl-PL\"", manifest, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Resx.Title\", \"pl-PL\"", manifest, StringComparison.Ordinal);

        string provider = GetGeneratedSource(result, "ProTranslateGeneratedTranslationProvider.g.cs");
        Assert.Contains("new ProTranslateGeneratedTranslationEntry(\"ArbTitle\", \"pl-PL\", \"Tytuł\")", provider, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateGeneratedTranslationEntry(\"Po.Title\", \"pl-PL\", \"Tytuł\")", provider, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateGeneratedTranslationEntry(\"Xliff.Title\", \"pl-PL\", \"Tytuł\")", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("@ArbTitle", provider, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyGeneratorPreservesFormatSpecificCultureAndValueShapes()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/app.arb",
                """
                {
                  "@@locale": "pl-PL",
                  "Arb.Title": "Tytuł",
                  "@Arb.Title": { "description": "Title" }
                }
                """),
            new InMemoryAdditionalText(
                "/translations/values-b+sr+Latn/strings.xml",
                """
                <resources>
                  <string name="Android_Title">Naslov</string>
                </resources>
                """),
            new InMemoryAdditionalText(
                "/translations/en.lproj/Localizable.strings",
                """
                "Apple.Escaped" = "Line\u0020One";
                """),
            new InMemoryAdditionalText(
                "/translations/Catalog.xcstrings",
                """
                {
                  "sourceLanguage": "en",
                  "strings": {
                    "Xc.Title": {
                      "localizations": {
                        "pl": {
                          "stringUnit": {
                            "state": "translated",
                            "value": "Tytuł"
                          }
                        }
                      }
                    },
                    "Xc.Files": {
                      "localizations": {
                        "pl": {
                          "variations": {
                            "plural": {
                              "one": {
                                "stringUnit": {
                                  "state": "translated",
                                  "value": "%lld plik"
                                }
                              },
                              "other": {
                                "stringUnit": {
                                  "state": "translated",
                                  "value": "%lld plików"
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  },
                  "version": "1.0"
                }
                """));

        Assert.Empty(result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        string manifest = GetGeneratedSource(result, "ProTranslateProviderManifest.g.cs");
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Android_Title\", \"sr-Latn\"", manifest, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Arb.Title\", \"pl-PL\"", manifest, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Apple.Escaped\", \"en\"", manifest, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Xc.Files.one\", \"pl\"", manifest, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Xc.Title\", \"pl\"", manifest, StringComparison.Ordinal);

        string provider = GetGeneratedSource(result, "ProTranslateGeneratedTranslationProvider.g.cs");
        Assert.Contains("new ProTranslateGeneratedTranslationEntry(\"Apple.Escaped\", \"en\", \"Line One\")", provider, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateGeneratedTranslationEntry(\"Xc.Title\", \"pl\", \"Tytuł\")", provider, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateGeneratedTranslationEntry(\"Xc.Files.other\", \"pl\", \"%lld plików\")", provider, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyGeneratorKeepsGettextPluralFormsDistinct()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/messages.pl-PL.po",
                """
                msgid ""
                msgstr ""
                "Language: pl-PL\n"
                "Plural-Forms: nplurals=3; plural=(n==1 ? 0 : n%10>=2 && n%10<=4 ? 1 : 2);\n"

                msgid "Files.Count"
                msgid_plural "Files.Count.Plural"
                msgstr[0] "{0} plik"
                msgstr[1] "{0} pliki"
                msgstr[2] "{0} plików"
                """));

        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Id == "PTSG002");

        string generated = GetGeneratedSource(result, "ProTranslateKeys.g.cs");
        Assert.Contains("public const string FilesCountOne = \"Files.Count.one\";", generated, StringComparison.Ordinal);
        Assert.Contains("public const string FilesCountFew = \"Files.Count.few\";", generated, StringComparison.Ordinal);
        Assert.Contains("public const string FilesCountMany = \"Files.Count.many\";", generated, StringComparison.Ordinal);

        string provider = GetGeneratedSource(result, "ProTranslateGeneratedTranslationProvider.g.cs");
        Assert.Contains("new ProTranslateGeneratedTranslationEntry(\"Files.Count.few\", \"pl-PL\", \"{0} pliki\")", provider, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateGeneratedTranslationEntry(\"Files.Count.many\", \"pl-PL\", \"{0} plików\")", provider, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyGeneratorReadsOnlyPluralRulesFromAppleStringsdict()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/en.lproj/Localizable.stringsdict",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                <plist version="1.0">
                <dict>
                  <key>Files.Count</key>
                  <dict>
                    <key>NSStringLocalizedFormatKey</key>
                    <string>%#@files@</string>
                    <key>files</key>
                    <dict>
                      <key>NSStringFormatSpecTypeKey</key>
                      <string>NSStringPluralRuleType</string>
                      <key>NSStringFormatValueTypeKey</key>
                      <string>d</string>
                      <key>one</key>
                      <string>%d file</string>
                      <key>other</key>
                      <string>%d files</string>
                    </dict>
                  </dict>
                  <key>Device.Width</key>
                  <dict>
                    <key>value</key>
                    <dict>
                      <key>NSStringFormatSpecTypeKey</key>
                      <string>NSStringVariableWidthRuleType</string>
                      <key>one</key>
                      <string>Narrow</string>
                    </dict>
                  </dict>
                </dict>
                </plist>
                """));

        string generated = GetGeneratedSource(result, "ProTranslateKeys.g.cs");
        Assert.Contains("public const string FilesCountOne = \"Files.Count.one\";", generated, StringComparison.Ordinal);
        Assert.Contains("public const string FilesCountOther = \"Files.Count.other\";", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("DeviceWidth", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyGeneratorReportsDuplicateKeysWithinAdditionalFile()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/Strings.en-US.json",
                """
                {
                  "Shell": {
                    "Title": "ProTranslate"
                  },
                  "Shell.Title": "Duplicate"
                }
                """));

        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(static diagnostic => diagnostic.Id == "PTSG002"));
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("Shell.Title", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void KeyGeneratorAllowsSameDelimitedKeyForDifferentCultures()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/catalog.csv",
                """
                key,culture,value
                Shell.Title,en-US,Title
                Shell.Title,pl-PL,Tytul
                """));

        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Id == "PTSG002");

        string provider = GetGeneratedSource(result, "ProTranslateGeneratedTranslationProvider.g.cs");
        Assert.Contains("new ProTranslateGeneratedTranslationEntry(\"Shell.Title\", \"en-US\", \"Title\")", provider, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateGeneratedTranslationEntry(\"Shell.Title\", \"pl-PL\", \"Tytul\")", provider, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedProviderKeepsDuplicateEntryPrecedenceIndependentOfValueText()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/A/Strings.en-US.json",
                """
                {
                  "Shell.Title": "Zulu"
                }
                """),
            new InMemoryAdditionalText(
                "/translations/B/Strings.en-US.json",
                """
                {
                  "Shell.Title": "Alpha"
                }
                """));

        string provider = GetGeneratedSource(result, "ProTranslateGeneratedTranslationProvider.g.cs");
        int baseIndex = provider.IndexOf("new ProTranslateGeneratedTranslationEntry(\"Shell.Title\", \"en-US\", \"Zulu\")", StringComparison.Ordinal);
        int overrideIndex = provider.IndexOf("new ProTranslateGeneratedTranslationEntry(\"Shell.Title\", \"en-US\", \"Alpha\")", StringComparison.Ordinal);

        Assert.True(baseIndex >= 0);
        Assert.True(overrideIndex >= 0);
        Assert.True(baseIndex < overrideIndex);
    }

    [Fact]
    public void KeyGeneratorReportsInvalidJsonCatalog()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText("/translations/Strings.en-US.json", "[]"));

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Id == "PTSG001");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("Strings.en-US.json", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void KeyGeneratorReportsInvalidJsonTokens()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/Strings.en-US.json",
                """
                {
                  "Shell.Title": nope
                }
                """));

        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(static diagnostic => diagnostic.Id == "PTSG001"));
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void KeyGeneratorIgnoresKeysInsideArrays()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/Strings.en-US.json",
                """
                {
                  "Shell": {
                    "Title": "ProTranslate"
                  },
                  "Metadata": [
                    {
                      "Ignored": "Value"
                    }
                  ]
                }
                """));

        Assert.Empty(result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        string generated = GetGeneratedSource(result, "ProTranslateKeys.g.cs");
        Assert.Contains("public const string ShellTitle = \"Shell.Title\";", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Ignored", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderManifestIsDeterministic()
    {
        AdditionalText appCatalog = new InMemoryAdditionalText(
            "/translations/App.protranslate.json",
            """
            {
              "Shell.Title": "ProTranslate"
            }
            """);

        AdditionalText textCatalog = new InMemoryAdditionalText(
            "/translations/Base.protranslate.keys.txt",
            """
            Orders.EmptyState
            Shell.FileMenu
            """);

        string first = GetGeneratedSource(RunGenerator(appCatalog, textCatalog), "ProTranslateProviderManifest.g.cs");
        string second = GetGeneratedSource(RunGenerator(textCatalog, appCatalog), "ProTranslateProviderManifest.g.cs");

        Assert.Equal(first, second);
        Assert.Contains("public static partial class ProTranslateProviderManifest", first, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Orders.EmptyState\", null, \"/translations/Base.protranslate.keys.txt\", global::System.Array.Empty<int>())", first, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Shell.Title\", null, \"/translations/App.protranslate.json\", global::System.Array.Empty<int>())", first, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderManifestInfersCulturesFromStringsJsonFiles()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/Strings.fr-FR.json",
                """
                {
                  "Shell.Title": "ProTranslate"
                }
                """),
            new InMemoryAdditionalText(
                "/translations/App.protranslate.json",
                """
                {
                  "Shared.App": "ProTranslate"
                }
                """));

        string generated = GetGeneratedSource(result, "ProTranslateProviderManifest.g.cs");

        Assert.Contains("new ProTranslateProviderManifestEntry(\"Shell.Title\", \"fr-FR\", \"/translations/Strings.fr-FR.json\", global::System.Array.Empty<int>())", generated, StringComparison.Ordinal);
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Shared.App\", null, \"/translations/App.protranslate.json\", global::System.Array.Empty<int>())", generated, StringComparison.Ordinal);
        Assert.Contains("\"fr-FR\",", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderManifestExtractsPlaceholderIndexesFromJsonValues()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText(
                "/translations/Strings.en-US.json",
                """
                {
                  "Orders": {
                    "Total": "Total {0:C} for {1} and {{2}} plus {10,4:X} and {Name}"
                  }
                }
                """));

        string generated = GetGeneratedSource(result, "ProTranslateProviderManifest.g.cs");

        Assert.Contains("new ProTranslateProviderManifestEntry(\"Orders.Total\", \"en-US\", \"/translations/Strings.en-US.json\", new int[] { 0, 1, 10 })", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("new int[] { 0, 1, 2, 10 }", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderManifestReportsInvalidJsonAndKeepsValidCatalogs()
    {
        GeneratorDriverRunResult result = RunGenerator(
            new InMemoryAdditionalText("/translations/Strings.en-US.json", "[]"),
            new InMemoryAdditionalText(
                "/translations/Strings.pl-PL.json",
                """
                {
                  "Shell.Title": "ProTranslate"
                }
                """));

        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(static diagnostic => diagnostic.Id == "PTSG001"));
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);

        string generated = GetGeneratedSource(result, "ProTranslateProviderManifest.g.cs");
        Assert.Contains("new ProTranslateProviderManifestEntry(\"Shell.Title\", \"pl-PL\", \"/translations/Strings.pl-PL.json\", global::System.Array.Empty<int>())", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("/translations/Strings.en-US.json", generated, StringComparison.Ordinal);
    }

    private static GeneratorDriverRunResult RunGenerator(params AdditionalText[] additionalTexts)
    {
        CSharpCompilation compilation = CreateCompilation(SourceText.From("namespace Demo { public sealed class App { } }", Encoding.UTF8));
        GeneratorDriver driver = CreateDriver(additionalTexts);
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result, string hintName) =>
        Assert.Single(
            result.Results.SelectMany(static generatorResult => generatorResult.GeneratedSources),
            source => string.Equals(source.HintName, hintName, StringComparison.Ordinal))
            .SourceText
            .ToString();

    private static GeneratorDriver CreateDriver(params AdditionalText[] additionalTexts) =>
        CSharpGeneratorDriver.Create(
            generators: [new ProTranslateKeysGenerator().AsSourceGenerator()],
            additionalTexts: additionalTexts.ToImmutableArray(),
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

    private static CSharpCompilation CreateCompilation(SourceText source)
    {
        IEnumerable<MetadataReference> references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .Concat([MetadataReference.CreateFromFile(typeof(ITranslationService).Assembly.Location)]);

        return CSharpCompilation.Create(
            "ProTranslate.Generated.Tests",
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedGeneratedStrings(
        Type stringsType,
        ITranslationService translations,
        CultureService cultures)
    {
        var strings = (IDisposable)Activator.CreateInstance(stringsType, translations)!;
        var weak = new WeakReference(strings);

        _ = stringsType.GetProperty("ShellTitle")?.GetValue(strings);
        cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));
        strings.Dispose();

        return weak;
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            _text = SourceText.From(text, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}
