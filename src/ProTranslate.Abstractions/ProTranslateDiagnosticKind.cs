namespace ProTranslate;

/// <summary>
/// Identifies ProTranslate diagnostic categories.
/// </summary>
public enum ProTranslateDiagnosticKind
{
    /// <summary>
    /// A resource key could not be resolved by the configured providers and fallback cultures.
    /// </summary>
    MissingTranslation,

    /// <summary>
    /// A translation provider failed while resolving a key.
    /// </summary>
    ProviderFailure,

    /// <summary>
    /// A localized format string could not be formatted with the supplied arguments.
    /// </summary>
    FormatFailure,

    /// <summary>
    /// A region profile could not be resolved from the supplied culture or region.
    /// </summary>
    RegionProfileFailure
}
