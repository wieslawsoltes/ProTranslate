using ProTranslate.Formats;
using System.Text.Json;

namespace ProTranslate.Tests;

public sealed class TranslationCatalogConverterTests
{
    [Fact]
    public void ImportsAndExportsXliff12()
    {
        const string xliff = """
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file source-language="en-US" target-language="pl-PL" datatype="plaintext" original="app">
                <body>
                  <trans-unit id="Shell.Title" resname="Shell.Title">
                    <source>Title</source>
                    <target state="translated">Tytuł</target>
                    <note>Window title</note>
                  </trans-unit>
                </body>
              </file>
            </xliff>
            """;

        TranslationFormatResult result = TranslationCatalogConverter.Import(xliff, TranslationFileFormat.Xliff12);

        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == TranslationFormatDiagnosticSeverity.Error);
        TranslationCatalogEntry entry = Assert.Single(result.Catalog.Entries);
        Assert.Equal("Shell.Title", entry.Key);
        Assert.Equal("pl-PL", entry.Culture);
        Assert.Equal("Tytuł", entry.Value);
        Assert.Equal("Title", entry.Source);
        Assert.Equal("Window title", entry.Comment);

        string exported = TranslationCatalogConverter.Export(result.Catalog, TranslationFileFormat.Xliff12, new TranslationFormatOptions { Culture = "pl-PL" });
        Assert.Contains("trans-unit", exported, StringComparison.Ordinal);
        Assert.Contains("Tytuł", exported, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportsResxAndroidApplePoArbAndDelimitedCatalogs()
    {
        AssertEntry(
            TranslationCatalogConverter.Import(
                """
                <root>
                  <data name="AppTitle" xml:space="preserve">
                    <value>ProTranslate</value>
                    <comment>Application title</comment>
                  </data>
                </root>
                """,
                TranslationFileFormat.Resx,
                new TranslationFormatOptions { Culture = "en-US" }),
            "AppTitle",
            "en-US",
            "ProTranslate");

        AssertEntry(
            TranslationCatalogConverter.Import(
                """
                <resources>
                  <string name="AppTitle">ProTranslate</string>
                  <plurals name="InboxCount">
                    <item quantity="one">%d message</item>
                    <item quantity="other">%d messages</item>
                  </plurals>
                </resources>
                """,
                TranslationFileFormat.AndroidResources,
                new TranslationFormatOptions { Culture = "en-US" }),
            "InboxCount.other",
            "en-US",
            "%d messages");

        AssertEntry(
            TranslationCatalogConverter.Import(
                """
                /* Application title */
                "AppTitle" = "ProTranslate";
                """,
                TranslationFileFormat.AppleStrings,
                new TranslationFormatOptions { Culture = "en-US" }),
            "AppTitle",
            "en-US",
            "ProTranslate");

        AssertEntry(
            TranslationCatalogConverter.Import(
                """
                msgid ""
                msgstr ""
                "Language: pl-PL\n"

                #. Application title
                msgid "AppTitle"
                msgstr "ProTranslate"
                """,
                TranslationFileFormat.GettextPo),
            "AppTitle",
            "pl-PL",
            "ProTranslate");

        AssertEntry(
            TranslationCatalogConverter.Import(
                """
                {
                  "@@locale": "en-US",
                  "AppTitle": "ProTranslate",
                  "@AppTitle": { "description": "Application title" }
                }
                """,
                TranslationFileFormat.FlutterArb),
            "AppTitle",
            "en-US",
            "ProTranslate");

        AssertEntry(
            TranslationCatalogConverter.Import(
                """
                key,culture,value,comment
                AppTitle,en-US,ProTranslate,Application title
                """,
                TranslationFileFormat.Csv),
            "AppTitle",
            "en-US",
            "ProTranslate");
    }

    [Fact]
    public void ImportsFlutterArbMetadataAsEntryComments()
    {
        TranslationFormatResult result = TranslationCatalogConverter.Import(
            """
            {
              "@@locale": "en-US",
              "AppTitle": "ProTranslate",
              "@AppTitle": {
                "description": "Application title"
              }
            }
            """,
            TranslationFileFormat.FlutterArb);

        TranslationCatalogEntry entry = Assert.Single(result.Catalog.Entries);
        Assert.Equal("AppTitle", entry.Key);
        Assert.Equal("Application title", entry.Comment);
    }

    [Fact]
    public void ExportsStableJsonAndDelimitedCatalogs()
    {
        var catalog = new TranslationCatalog();
        catalog.Add("Shell.Title", "en-US", "ProTranslate");
        catalog.Add("Orders.Total", "en-US", "Total: {0:C}");

        string json = TranslationCatalogConverter.Export(catalog, TranslationFileFormat.ProTranslateJson, new TranslationFormatOptions { Culture = "en-US" });
        Assert.Contains("\"Orders.Total\": \"Total: {0:C}\"", json, StringComparison.Ordinal);
        Assert.Contains("\"Shell.Title\": \"ProTranslate\"", json, StringComparison.Ordinal);

        string csv = TranslationCatalogConverter.Export(catalog, TranslationFileFormat.Csv);
        Assert.StartsWith("key,culture,value,source,comment,context,state,plural,format", csv, StringComparison.Ordinal);
        Assert.Contains("Orders.Total,en-US,Total: {0:C}", csv, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportPrefersRequestedCultureOverNeutralFallback()
    {
        var catalog = new TranslationCatalog();
        catalog.Add("Shell.Title", null, "Neutral title");
        catalog.Add("Shell.Title", "pl-PL", "Tytuł");
        catalog.Add("Shell.Subtitle", null, "Neutral subtitle");

        string json = TranslationCatalogConverter.Export(catalog, TranslationFileFormat.ProTranslateJson, new TranslationFormatOptions { Culture = "pl-PL" });

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal("Tytuł", document.RootElement.GetProperty("Shell.Title").GetString());
        Assert.Equal("Neutral subtitle", document.RootElement.GetProperty("Shell.Subtitle").GetString());
        Assert.DoesNotContain("Neutral title", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportsXcstringsAndExportsGeneratedProviderFriendlyCatalog()
    {
        const string xcstrings = """
            {
              "sourceLanguage": "en",
              "strings": {
                "Shell.Title": {
                  "localizations": {
                    "en-US": {
                      "stringUnit": {
                        "state": "translated",
                        "value": "ProTranslate"
                      }
                    }
                  }
                }
              },
              "version": "1.0"
            }
            """;

        TranslationFormatResult result = TranslationCatalogConverter.Import(xcstrings, TranslationFileFormat.AppleXcstrings);

        TranslationCatalogEntry entry = Assert.Single(result.Catalog.Entries);
        Assert.Equal("Shell.Title", entry.Key);
        Assert.Equal("en-US", entry.Culture);
        Assert.Equal("translated", entry.State);
        Assert.Equal("ProTranslate", entry.Value);

        string exported = TranslationCatalogConverter.Export(result.Catalog, TranslationFileFormat.AppleXcstrings);
        Assert.Contains("\"Shell.Title\"", exported, StringComparison.Ordinal);
        Assert.Contains("\"value\": \"ProTranslate\"", exported, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportsAndExportsXcstringsPluralVariations()
    {
        const string xcstrings = """
            {
              "sourceLanguage": "en",
              "strings": {
                "Inbox.Count": {
                  "comment": "Inbox count",
                  "localizations": {
                    "en": {
                      "variations": {
                        "plural": {
                          "one": {
                            "stringUnit": {
                              "state": "translated",
                              "value": "%d message"
                            }
                          },
                          "other": {
                            "stringUnit": {
                              "state": "translated",
                              "value": "%d messages"
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
            """;

        TranslationFormatResult result = TranslationCatalogConverter.Import(xcstrings, TranslationFileFormat.AppleXcstrings);

        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == TranslationFormatDiagnosticSeverity.Error);
        TranslationCatalogEntry one = Assert.Single(result.Catalog.Entries, static entry => entry.PluralCategory == "one");
        TranslationCatalogEntry other = Assert.Single(result.Catalog.Entries, static entry => entry.PluralCategory == "other");
        Assert.Equal("Inbox.Count.one", one.Key);
        Assert.Equal("Inbox count", one.Comment);
        Assert.Equal("%d message", one.Value);
        Assert.Equal("Inbox.Count.other", other.Key);
        Assert.Equal("%d messages", other.Value);

        string exported = TranslationCatalogConverter.Export(result.Catalog, TranslationFileFormat.AppleXcstrings, new TranslationFormatOptions { Culture = "en" });
        Assert.Contains("\"variations\"", exported, StringComparison.Ordinal);
        Assert.Contains("\"plural\"", exported, StringComparison.Ordinal);
        Assert.Contains("\"one\"", exported, StringComparison.Ordinal);
        Assert.Contains("\"other\"", exported, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportsGettextPluralEntriesAsPluralForms()
    {
        var catalog = new TranslationCatalog();
        catalog.Add("Inbox.Count.one", "pl-PL", "%d wiadomość").Source = "%d message";
        TranslationCatalogEntry other = catalog.Add("Inbox.Count.other", "pl-PL", "%d wiadomości");
        other.Source = "%d messages";
        foreach (TranslationCatalogEntry entry in catalog.Entries)
        {
            entry.PluralCategory = entry.Key.EndsWith(".one", StringComparison.Ordinal) ? "one" : "other";
            entry.Comment = "Inbox count";
        }

        string exported = TranslationCatalogConverter.Export(catalog, TranslationFileFormat.GettextPo, new TranslationFormatOptions { Culture = "pl-PL" });

        Assert.Contains("msgid \"%d message\"", exported, StringComparison.Ordinal);
        Assert.Contains("msgid_plural \"%d messages\"", exported, StringComparison.Ordinal);
        Assert.Contains("msgstr[0] \"%d wiadomość\"", exported, StringComparison.Ordinal);
        Assert.Contains("msgstr[1] \"%d wiadomości\"", exported, StringComparison.Ordinal);

        TranslationFormatResult result = TranslationCatalogConverter.Import(exported, TranslationFileFormat.GettextPo);
        AssertEntry(result, "%d message.one", "pl-PL", "%d wiadomość");
        AssertEntry(result, "%d message.other", "pl-PL", "%d wiadomości");
    }

    private static void AssertEntry(TranslationFormatResult result, string key, string? culture, string value)
    {
        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == TranslationFormatDiagnosticSeverity.Error);
        TranslationCatalogEntry entry = Assert.Single(result.Catalog.Entries, entry => string.Equals(entry.Key, key, StringComparison.Ordinal));
        Assert.Equal(culture, entry.Culture);
        Assert.Equal(value, entry.Value);
    }
}
