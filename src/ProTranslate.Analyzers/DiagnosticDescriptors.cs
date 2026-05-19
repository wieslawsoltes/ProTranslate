using Microsoft.CodeAnalysis;

namespace ProTranslate.Analyzers;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor MissingStaticKey = new(
        "PTA001",
        "Missing static ProTranslate key",
        "Translation key '{0}' was not found in ProTranslate catalogs",
        "ProTranslate.Analyzers",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Static ProTranslate keys should exist in the configured catalogs.");

    public static readonly DiagnosticDescriptor PlaceholderCountMismatch = new(
        "PTA002",
        "ProTranslate placeholder count mismatch",
        "Translation key '{0}' expects {1} format argument(s), but this call provides {2}",
        "ProTranslate.Analyzers",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Static ProTranslate Format calls should pass the number of arguments required by the catalog value.");

    public static readonly DiagnosticDescriptor ResourceCoverageGap = new(
        "PTA003",
        "ProTranslate resource coverage gap",
        "Catalog '{0}' is missing translation key '{1}'",
        "ProTranslate.Analyzers",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Culture-specific ProTranslate JSON catalogs should cover the same translation keys.");

    public static readonly DiagnosticDescriptor UnsafeDynamicKey = new(
        "PTA004",
        "Unsafe dynamic ProTranslate key",
        "Translation key argument is not a compile-time constant and cannot be validated",
        "ProTranslate.Analyzers",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Dynamic ProTranslate keys cannot be validated against catalogs at compile time.");

    public static readonly DiagnosticDescriptor InvalidCatalog = new(
        "PTA005",
        "Invalid ProTranslate catalog",
        "Could not parse ProTranslate catalog '{0}': {1}",
        "ProTranslate.Analyzers",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ProTranslate catalog AdditionalFiles must be valid key lists or JSON root objects.");
}
