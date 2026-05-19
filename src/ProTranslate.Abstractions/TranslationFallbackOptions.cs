using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Controls culture fallback behavior for translation lookups.
/// </summary>
public sealed class TranslationFallbackOptions
{
    /// <summary>
    /// Gets or sets the default culture used after the requested culture and parent cultures miss.
    /// </summary>
    public CultureInfo DefaultCulture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Gets the explicit fallback cultures to try after the requested culture parents.
    /// </summary>
    public IList<CultureInfo> FallbackCultures { get; } = new List<CultureInfo>();

    /// <summary>
    /// Gets or sets a value indicating whether parent cultures should be tried.
    /// </summary>
    public bool UseParentCultures { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DefaultCulture"/> should be tried.
    /// </summary>
    public bool UseDefaultCulture { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether missing translations should return the key as the value.
    /// </summary>
    public bool ReturnKeyWhenMissing { get; set; } = true;

    /// <summary>
    /// Gets or sets how provider exceptions are handled during lookup.
    /// </summary>
    public TranslationProviderFailureBehavior ProviderFailureBehavior { get; set; } =
        TranslationProviderFailureBehavior.ReportAndContinue;

    /// <summary>
    /// Gets or sets how formatting failures are handled.
    /// </summary>
    public TranslationFormatFailureBehavior FormatFailureBehavior { get; set; } =
        TranslationFormatFailureBehavior.ReportAndReturnUnformatted;
}
