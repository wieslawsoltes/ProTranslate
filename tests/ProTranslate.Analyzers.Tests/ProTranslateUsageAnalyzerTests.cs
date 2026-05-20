using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ProTranslate.Analyzers;

namespace ProTranslate.Analyzers.Tests;

public sealed class ProTranslateUsageAnalyzerTests
{
    [Fact]
    public async Task ReportsMissingStaticKey()
    {
        const string source = """
            using ProTranslate;

            namespace Demo;

            public static class Usage
            {
                public static string Read(ITranslationService translations)
                {
                    _ = translations.GetString("Shell.Title");
                    return translations.GetString("Shell.Missing").Value;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(
            source,
            JsonCatalog(
                "/translations/Strings.en-US.json",
                """
                {
                  "Shell.Title": "ProTranslate"
                }
                """));

        Diagnostic diagnostic = Assert.Single(diagnostics.Where(static diagnostic => diagnostic.Id == "PTA001"));
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(FindLine(source, "Shell.Missing"), diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        Assert.Contains("Shell.Missing", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsUnsafeDynamicKeyAndAllowsConstKey()
    {
        const string source = """
            using ProTranslate;

            namespace Demo;

            public static class Usage
            {
                public static void Read(ITranslationService translations, string suffix)
                {
                    const string StaticKey = "Shell.Title";
                    string dynamicKey = "Shell." + suffix;

                    _ = translations.GetString(StaticKey);
                    _ = translations.Observe(dynamicKey);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(
            source,
            JsonCatalog(
                "/translations/Strings.en-US.json",
                """
                {
                  "Shell.Title": "ProTranslate"
                }
                """));

        Diagnostic diagnostic = Assert.Single(diagnostics.Where(static diagnostic => diagnostic.Id == "PTA004"));
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(FindLine(source, "translations.Observe(dynamicKey)"), diagnostic.Location.GetLineSpan().StartLinePosition.Line);
    }

    [Fact]
    public async Task ReportsPlaceholderMismatchForStaticFormatCall()
    {
        const string source = """
            using ProTranslate;

            namespace Demo;

            public static class Usage
            {
                public static string Read(ITranslationService translations)
                {
                    return translations.Format("Orders.Total", 12.5m);
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(
            source,
            JsonCatalog(
                "/translations/Strings.en-US.json",
                """
                {
                  "Orders.Total": "Total: {0:C} on {1:d}"
                }
                """));

        Diagnostic diagnostic = Assert.Single(diagnostics.Where(static diagnostic => diagnostic.Id == "PTA002"));
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(FindLine(source, "Orders.Total"), diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        Assert.Contains("expects 2", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("provides 1", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsResourceCoverageAcrossJsonCultureCatalogs()
    {
        const string source = """
            namespace Demo;

            public sealed class App
            {
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(
            source,
            JsonCatalog(
                "/translations/Strings.en-US.json",
                """
                {
                  "Shell.Title": "ProTranslate",
                  "Orders.Total": "Total: {0:C}"
                }
                """),
            JsonCatalog(
                "/translations/Strings.pl-PL.json",
                """
                {
                  "Shell.Title": "ProTranslate"
                }
                """));

        Diagnostic diagnostic = Assert.Single(diagnostics.Where(static diagnostic => diagnostic.Id == "PTA003"));
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.EndsWith("Strings.pl-PL.json", diagnostic.Location.GetLineSpan().Path, StringComparison.Ordinal);
        Assert.Equal(0, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        Assert.Contains("Orders.Total", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecognizesIndexerAdapterAndGeneratedAccessorShapes()
    {
        const string source = """
            using ProTranslate;
            using ProTranslate.Generated;

            namespace ProTranslate.Avalonia
            {
                public static class TranslationService
                {
                    public static string T(string key) => key;
                }
            }

            namespace ProTranslate.Generated
            {
                public static class ProTranslateAccessors
                {
                    public static string Format_OrdersTotal(this ITranslationService translations, params object?[] arguments) => string.Empty;
                }
            }

            namespace Demo
            {
                public static class Usage
                {
                    public static string Read(ITranslationService translations)
                    {
                        _ = translations["Shell.Missing"];
                        _ = ProTranslate.Avalonia.TranslationService.T("Adapter.Missing");
                        return translations.Format_OrdersTotal(12.5m);
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(
            source,
            JsonCatalog(
                "/translations/Strings.en-US.json",
                """
                {
                  "Orders.Total": "Total: {0:C} on {1:d}",
                  "Shell.Title": "ProTranslate"
                }
                """));

        Assert.Equal(2, diagnostics.Count(static diagnostic => diagnostic.Id == "PTA001"));
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "PTA001" && diagnostic.GetMessage().Contains("Shell.Missing", StringComparison.Ordinal));
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "PTA001" && diagnostic.GetMessage().Contains("Adapter.Missing", StringComparison.Ordinal));

        Diagnostic placeholder = Assert.Single(diagnostics.Where(static diagnostic => diagnostic.Id == "PTA002"));
        Assert.Equal(FindLine(source, "translations.Format_OrdersTotal"), placeholder.Location.GetLineSpan().StartLinePosition.Line);
    }

    [Fact]
    public async Task ReportsInvalidCatalogAdditionalFile()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunAnalyzerAsync(
            "namespace Demo; public sealed class App { }",
            new InMemoryAdditionalText("/translations/Strings.en-US.json", "[]"));

        Diagnostic diagnostic = Assert.Single(diagnostics.Where(static diagnostic => diagnostic.Id == "PTA005"));
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.EndsWith("Strings.en-US.json", diagnostic.Location.GetLineSpan().Path, StringComparison.Ordinal);
    }

    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(string source, params AdditionalText[] additionalTexts)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        ImmutableArray<Diagnostic> compilationErrors = compilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        Assert.Empty(compilationErrors);

        var options = new AnalyzerOptions(additionalTexts.ToImmutableArray());
        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new ProTranslateUsageAnalyzer()),
            new CompilationWithAnalyzersOptions(
                options,
                onAnalyzerException: null,
                concurrentAnalysis: false,
                logAnalyzerExecutionTime: false,
                reportSuppressedDiagnostics: false));

        ImmutableArray<Diagnostic> diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics
            .OrderBy(static diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Location.GetLineSpan().Path, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToImmutableArray();
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        IEnumerable<MetadataReference> references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .Concat([MetadataReference.CreateFromFile(typeof(ITranslationService).Assembly.Location)]);

        return CSharpCompilation.Create(
            "ProTranslate.Analyzers.Tests.Target",
            [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static int FindLine(string source, string text)
    {
        int index = source.IndexOf(text, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Could not find '{text}' in source.");
        return source[..index].Count(static character => character == '\n');
    }

    private static InMemoryAdditionalText JsonCatalog(string path, string text) => new(path, text);

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
