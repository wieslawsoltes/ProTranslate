using System.Globalization;
using Microsoft.Extensions.Localization;

namespace ProTranslate;

/// <summary>
/// Provides translations through Microsoft.Extensions.Localization.
/// </summary>
public sealed class StringLocalizerTranslationProvider : ITranslationProvider
{
    private readonly IStringLocalizer _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringLocalizerTranslationProvider"/> class.
    /// </summary>
    /// <param name="localizer">The string localizer.</param>
    public StringLocalizerTranslationProvider(IStringLocalizer localizer)
        : this(localizer, "IStringLocalizer")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringLocalizerTranslationProvider"/> class.
    /// </summary>
    /// <param name="localizer">The string localizer.</param>
    /// <param name="name">The provider name.</param>
    public StringLocalizerTranslationProvider(IStringLocalizer localizer, string name)
    {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public LocalizedString GetString(string key, CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(culture);

        CultureInfo oldCulture = CultureInfo.CurrentCulture;
        CultureInfo oldUICulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            Microsoft.Extensions.Localization.LocalizedString value = _localizer[key];
            return new LocalizedString(key, value.Value, culture, value.ResourceNotFound, Name);
        }
        finally
        {
            CultureInfo.CurrentCulture = oldCulture;
            CultureInfo.CurrentUICulture = oldUICulture;
        }
    }
}
