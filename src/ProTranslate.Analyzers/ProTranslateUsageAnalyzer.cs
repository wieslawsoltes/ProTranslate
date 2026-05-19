using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ProTranslate.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProTranslateUsageAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> AdapterNamespaces = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "ProTranslate.Avalonia",
        "ProTranslate.Wpf",
        "ProTranslate.Maui",
        "ProTranslate.WinUI",
        "ProTranslate.Uno");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.MissingStaticKey,
        DiagnosticDescriptors.PlaceholderCountMismatch,
        DiagnosticDescriptors.ResourceCoverageGap,
        DiagnosticDescriptors.UnsafeDynamicKey,
        DiagnosticDescriptors.InvalidCatalog);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(InitializeCompilation);
    }

    private static void InitializeCompilation(CompilationStartAnalysisContext context)
    {
        CatalogModel catalog = CatalogModel.Create(context.Options.AdditionalFiles, context.CancellationToken);
        INamedTypeSymbol? translationServiceType = context.Compilation.GetTypeByMetadataName("ProTranslate.ITranslationService");
        INamedTypeSymbol? globalizationServiceType = context.Compilation.GetTypeByMetadataName("ProTranslate.IGlobalizationService");
        Dictionary<string, string> generatedAccessorMap = KeyIdentifierHelpers.CreateGeneratedAccessorMap(catalog.Keys.Keys);

        context.RegisterCompilationEndAction(endContext =>
        {
            foreach (Diagnostic diagnostic in catalog.Diagnostics)
            {
                endContext.ReportDiagnostic(diagnostic);
            }
        });

        context.RegisterSyntaxNodeAction(
            syntaxContext => AnalyzeInvocation(syntaxContext, catalog, translationServiceType, globalizationServiceType, generatedAccessorMap),
            SyntaxKind.InvocationExpression);

        context.RegisterSyntaxNodeAction(
            syntaxContext => AnalyzeElementAccess(syntaxContext, catalog, translationServiceType),
            SyntaxKind.ElementAccessExpression);
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        CatalogModel catalog,
        INamedTypeSymbol? translationServiceType,
        INamedTypeSymbol? globalizationServiceType,
        Dictionary<string, string> generatedAccessorMap)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
        {
            return;
        }

        KeyUsage? usage = TryCreateGeneratedAccessorUsage(context, catalog, invocation, method, generatedAccessorMap);
        if (usage is null)
        {
            usage = TryCreateMethodUsage(context, catalog, invocation, method, translationServiceType, globalizationServiceType);
        }

        if (usage is null)
        {
            return;
        }

        AnalyzeUsage(context, catalog, usage);
    }

    private static void AnalyzeElementAccess(
        SyntaxNodeAnalysisContext context,
        CatalogModel catalog,
        INamedTypeSymbol? translationServiceType)
    {
        var elementAccess = (ElementAccessExpressionSyntax)context.Node;
        if (elementAccess.ArgumentList.Arguments.Count == 0)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(elementAccess, context.CancellationToken).Symbol is not IPropertySymbol property
            || !property.IsIndexer
            || property.Parameters.Length == 0
            || property.Parameters[0].Type.SpecialType != SpecialType.System_String)
        {
            return;
        }

        if (!IsTranslationServiceLike(property.ContainingType, translationServiceType)
            && !IsAdapterBindingSource(property.ContainingType))
        {
            return;
        }

        ArgumentSyntax keyArgument = elementAccess.ArgumentList.Arguments[0];
        AnalyzeUsage(
            context,
            catalog,
            KeyUsage.FromKeyExpression(keyArgument.Expression, UsageKind.Lookup, formatArgumentCount: null));
    }

    private static KeyUsage? TryCreateMethodUsage(
        SyntaxNodeAnalysisContext context,
        CatalogModel catalog,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        INamedTypeSymbol? translationServiceType,
        INamedTypeSymbol? globalizationServiceType)
    {
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 0 || method.Parameters.Length == 0 || method.Parameters[0].Type.SpecialType != SpecialType.System_String)
        {
            return null;
        }

        if (IsTranslationLookupMethod(method, translationServiceType, globalizationServiceType))
        {
            UsageKind kind = method.Name == "Format" ? UsageKind.Format : UsageKind.Lookup;
            int? formatCount = kind == UsageKind.Format
                ? TryGetFormatArgumentCount(context, method, arguments, firstFormatArgumentIndex: 1)
                : null;

            return KeyUsage.FromKeyExpression(arguments[0].Expression, kind, formatCount);
        }

        if (IsAdapterStaticMethod(method))
        {
            return KeyUsage.FromKeyExpression(arguments[0].Expression, UsageKind.Lookup, formatArgumentCount: null);
        }

        if (IsAdapterBindingSourceTranslate(method))
        {
            int? formatCount = TryGetFormatArgumentCount(context, method, arguments, firstFormatArgumentIndex: 1);
            return KeyUsage.FromKeyExpression(arguments[0].Expression, UsageKind.Format, formatCount);
        }

        return null;
    }

    private static KeyUsage? TryCreateGeneratedAccessorUsage(
        SyntaxNodeAnalysisContext context,
        CatalogModel catalog,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        Dictionary<string, string> generatedAccessorMap)
    {
        _ = catalog;

        if (!IsGeneratedAccessorMethod(method) || !generatedAccessorMap.TryGetValue(method.Name, out string key))
        {
            return null;
        }

        UsageKind kind = method.Name.StartsWith("Format_", StringComparison.Ordinal)
            ? UsageKind.Format
            : UsageKind.Lookup;

        int? formatCount = null;
        if (kind == UsageKind.Format)
        {
            int firstFormatArgumentIndex = method.ReducedFrom is null ? 1 : 0;
            formatCount = TryGetFormatArgumentCount(context, method, invocation.ArgumentList.Arguments, firstFormatArgumentIndex);
        }

        return KeyUsage.FromResolvedKey(key, invocation.Expression.GetLocation(), kind, formatCount);
    }

    private static void AnalyzeUsage(SyntaxNodeAnalysisContext context, CatalogModel catalog, KeyUsage usage)
    {
        string? key = usage.Key;

        if (key is null)
        {
            Optional<object?> constant = context.SemanticModel.GetConstantValue(usage.KeyExpression!, context.CancellationToken);
            if (!constant.HasValue || constant.Value is not string constantKey)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsafeDynamicKey,
                    usage.Location));
                return;
            }

            key = constantKey;
        }

        string resolvedKey = key;

        if (catalog.HasKeys && !catalog.Keys.ContainsKey(resolvedKey))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MissingStaticKey,
                usage.Location,
                resolvedKey));
            return;
        }

        if (usage.Kind != UsageKind.Format || usage.FormatArgumentCount is null)
        {
            return;
        }

        if (!catalog.PlaceholderCounts.TryGetValue(resolvedKey, out ImmutableHashSet<int>? counts) || counts.Count != 1)
        {
            return;
        }

        int expected = counts.Single();
        int actual = usage.FormatArgumentCount.Value;
        if (expected == actual)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PlaceholderCountMismatch,
            usage.Location,
            resolvedKey,
            expected,
            actual));
    }

    private static bool IsTranslationLookupMethod(
        IMethodSymbol method,
        INamedTypeSymbol? translationServiceType,
        INamedTypeSymbol? globalizationServiceType)
    {
        if (method.Name != "GetString" && method.Name != "Format" && method.Name != "Observe")
        {
            return false;
        }

        return IsTranslationServiceLike(method.ContainingType, translationServiceType)
            || IsSameOrImplements(method.ContainingType, globalizationServiceType);
    }

    private static bool IsTranslationServiceLike(ITypeSymbol type, INamedTypeSymbol? translationServiceType)
    {
        return IsSameOrImplements(type, translationServiceType);
    }

    private static bool IsSameOrImplements(ITypeSymbol type, INamedTypeSymbol? interfaceType)
    {
        if (interfaceType is null)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(type, interfaceType))
        {
            return true;
        }

        foreach (INamedTypeSymbol implementedInterface in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implementedInterface, interfaceType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAdapterStaticMethod(IMethodSymbol method)
    {
        return method.IsStatic
            && (method.Name == "Translate" || method.Name == "T")
            && method.ContainingType?.Name == "TranslationService"
            && AdapterNamespaces.Contains(GetNamespace(method.ContainingType))
            && method.Parameters.Length > 0
            && method.Parameters[0].Type.SpecialType == SpecialType.System_String;
    }

    private static bool IsAdapterBindingSourceTranslate(IMethodSymbol method)
    {
        return method.Name == "Translate"
            && method.ContainingType?.Name == "TranslationBindingSource"
            && AdapterNamespaces.Contains(GetNamespace(method.ContainingType))
            && method.Parameters.Length > 0
            && method.Parameters[0].Type.SpecialType == SpecialType.System_String;
    }

    private static bool IsAdapterBindingSource(ITypeSymbol type)
    {
        return type.Name == "TranslationBindingSource"
            && AdapterNamespaces.Contains(GetNamespace(type));
    }

    private static bool IsGeneratedAccessorMethod(IMethodSymbol method)
    {
        return method.ContainingType?.Name == "ProTranslateAccessors"
            && GetNamespace(method.ContainingType) == "ProTranslate.Generated"
            && (method.Name.StartsWith("Get_", StringComparison.Ordinal)
                || method.Name.StartsWith("Value_", StringComparison.Ordinal)
                || method.Name.StartsWith("Format_", StringComparison.Ordinal)
                || method.Name.StartsWith("Observe_", StringComparison.Ordinal));
    }

    private static int? TryGetFormatArgumentCount(
        SyntaxNodeAnalysisContext context,
        IMethodSymbol method,
        SeparatedSyntaxList<ArgumentSyntax> arguments,
        int firstFormatArgumentIndex)
    {
        if (firstFormatArgumentIndex >= arguments.Count)
        {
            return 0;
        }

        int formatArgumentCount = arguments.Count - firstFormatArgumentIndex;
        if (formatArgumentCount == 1)
        {
            ExpressionSyntax expression = arguments[firstFormatArgumentIndex].Expression;
            if (expression is ArrayCreationExpressionSyntax arrayCreation)
            {
                return arrayCreation.Initializer?.Expressions.Count;
            }

            if (expression is ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
            {
                return implicitArrayCreation.Initializer.Expressions.Count;
            }

            ITypeSymbol? type = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).ConvertedType
                ?? context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).Type;

            if (type is IArrayTypeSymbol && MethodHasParamsArray(method))
            {
                return null;
            }
        }

        return formatArgumentCount;
    }

    private static bool MethodHasParamsArray(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            return false;
        }

        return method.Parameters[method.Parameters.Length - 1].IsParams;
    }

    private static string GetNamespace(ISymbol symbol)
    {
        if (symbol.ContainingNamespace is null || symbol.ContainingNamespace.IsGlobalNamespace)
        {
            return string.Empty;
        }

        return symbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
    }

    private enum UsageKind
    {
        Lookup,
        Format
    }

    private sealed class KeyUsage
    {
        private KeyUsage(string? key, ExpressionSyntax? keyExpression, Location location, UsageKind kind, int? formatArgumentCount)
        {
            Key = key;
            KeyExpression = keyExpression;
            Location = location;
            Kind = kind;
            FormatArgumentCount = formatArgumentCount;
        }

        public string? Key { get; }

        public ExpressionSyntax? KeyExpression { get; }

        public Location Location { get; }

        public UsageKind Kind { get; }

        public int? FormatArgumentCount { get; }

        public static KeyUsage FromKeyExpression(ExpressionSyntax keyExpression, UsageKind kind, int? formatArgumentCount)
        {
            return new KeyUsage(null, keyExpression, keyExpression.GetLocation(), kind, formatArgumentCount);
        }

        public static KeyUsage FromResolvedKey(string key, Location location, UsageKind kind, int? formatArgumentCount)
        {
            return new KeyUsage(key, null, location, kind, formatArgumentCount);
        }
    }
}
