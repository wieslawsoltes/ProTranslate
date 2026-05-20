using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Tries multiple translation providers in order.
/// </summary>
public sealed class CompositeTranslationProvider : ITranslationProvider
{
    private readonly IReadOnlyList<ITranslationProvider> _providers;
    private readonly TranslationProviderFailureBehavior _providerFailureBehavior;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeTranslationProvider"/> class.
    /// </summary>
    /// <param name="providers">The ordered providers.</param>
    public CompositeTranslationProvider(IEnumerable<ITranslationProvider> providers)
        : this(providers, TranslationProviderFailureBehavior.ReportAndContinue)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeTranslationProvider"/> class.
    /// </summary>
    /// <param name="providers">The ordered providers.</param>
    /// <param name="providerFailureBehavior">The provider failure behavior.</param>
    public CompositeTranslationProvider(
        IEnumerable<ITranslationProvider> providers,
        TranslationProviderFailureBehavior providerFailureBehavior)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers.ToArray();
        _providerFailureBehavior = providerFailureBehavior;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeTranslationProvider"/> class.
    /// </summary>
    /// <param name="providers">The ordered providers.</param>
    public CompositeTranslationProvider(params ITranslationProvider[] providers)
        : this((IEnumerable<ITranslationProvider>)providers)
    {
    }

    /// <inheritdoc />
    public string Name => "Composite";

    /// <summary>
    /// Gets the ordered providers.
    /// </summary>
    public IReadOnlyList<ITranslationProvider> Providers => _providers;

    /// <inheritdoc />
    public LocalizedString GetString(string key, CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(culture);

        List<ProTranslateDiagnostic>? diagnostics = null;
        foreach (ITranslationProvider provider in _providers)
        {
            LocalizedString localized;
            try
            {
                localized = provider.GetString(key, culture);
            }
            catch (Exception ex)
            {
                ProTranslateDiagnostic diagnostic = CreateProviderFailureDiagnostic(key, culture, provider.Name, ex);
                if (_providerFailureBehavior == TranslationProviderFailureBehavior.ReportAndThrow)
                {
                    throw;
                }

                diagnostics ??= new List<ProTranslateDiagnostic>();
                diagnostics.Add(diagnostic);
                continue;
            }

            if (localized.Diagnostics.Count > 0)
            {
                diagnostics ??= new List<ProTranslateDiagnostic>();
                diagnostics.AddRange(localized.Diagnostics);
            }

            if (!localized.ResourceNotFound)
            {
                return diagnostics is null
                    ? localized
                    : localized with { Diagnostics = diagnostics };
            }
        }

        return new LocalizedString(key, key, culture, true, Name)
        {
            Diagnostics = diagnostics ?? (IReadOnlyList<ProTranslateDiagnostic>)Array.Empty<ProTranslateDiagnostic>()
        };
    }

    private static ProTranslateDiagnostic CreateProviderFailureDiagnostic(
        string key,
        CultureInfo culture,
        string providerName,
        Exception exception) =>
        new(
            ProTranslateDiagnosticKind.ProviderFailure,
            ProTranslateDiagnosticSeverity.Error,
            $"Translation provider '{providerName}' failed while resolving key '{key}' for culture '{culture.Name}'.",
            key,
            culture,
            providerName,
            exception);
}
