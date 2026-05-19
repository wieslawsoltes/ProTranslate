namespace ProTranslate;

/// <summary>
/// Controls how localized formatting handles invalid format strings or arguments.
/// </summary>
public enum TranslationFormatFailureBehavior
{
    /// <summary>
    /// Report the failure as a diagnostic and return the unformatted localized value.
    /// </summary>
    ReportAndReturnUnformatted,

    /// <summary>
    /// Report the failure as a diagnostic and rethrow the formatting exception.
    /// </summary>
    ReportAndThrow
}
