namespace ProTranslate;

/// <summary>
/// Controls how translation lookup handles provider exceptions.
/// </summary>
public enum TranslationProviderFailureBehavior
{
    /// <summary>
    /// Report the failure as a diagnostic and continue fallback lookup.
    /// </summary>
    ReportAndContinue,

    /// <summary>
    /// Report the failure as a diagnostic and rethrow the provider exception.
    /// </summary>
    ReportAndThrow
}
