using System.Globalization;
using System.Collections.Concurrent;

namespace ProTranslate;

/// <summary>
/// Default implementation of <see cref="ITranslationService"/>.
/// </summary>
public sealed class TranslationService : ITranslationService, ITranslationCacheInvalidator, IDisposable
{
    private readonly ITranslationProvider _provider;
    private readonly ICultureService _cultureService;
    private readonly TranslationFallbackOptions _options;
    private readonly TranslationCacheOptions _cacheOptions;
    private readonly IProTranslateDiagnosticSink? _diagnosticSink;
    private readonly ConcurrentDictionary<TranslationCacheKey, LocalizedString> _cache = new();
    private readonly object _cachePolicyLock = new();
    private string _fallbackPolicySignature;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationService"/> class.
    /// </summary>
    /// <param name="provider">The translation provider.</param>
    /// <param name="cultureService">The culture service.</param>
    public TranslationService(ITranslationProvider provider, ICultureService cultureService)
        : this(provider, cultureService, new TranslationFallbackOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationService"/> class.
    /// </summary>
    /// <param name="provider">The translation provider.</param>
    /// <param name="cultureService">The culture service.</param>
    /// <param name="options">The fallback options.</param>
    /// <param name="diagnosticSink">The optional diagnostic sink.</param>
    public TranslationService(
        ITranslationProvider provider,
        ICultureService cultureService,
        TranslationFallbackOptions options,
        IProTranslateDiagnosticSink? diagnosticSink = null)
        : this(provider, cultureService, options, new TranslationCacheOptions(), diagnosticSink)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationService"/> class.
    /// </summary>
    /// <param name="provider">The translation provider.</param>
    /// <param name="cultureService">The culture service.</param>
    /// <param name="options">The fallback options.</param>
    /// <param name="cacheOptions">The cache options.</param>
    /// <param name="diagnosticSink">The optional diagnostic sink.</param>
    public TranslationService(
        ITranslationProvider provider,
        ICultureService cultureService,
        TranslationFallbackOptions options,
        TranslationCacheOptions cacheOptions,
        IProTranslateDiagnosticSink? diagnosticSink = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _cultureService = cultureService ?? throw new ArgumentNullException(nameof(cultureService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _cacheOptions = cacheOptions ?? throw new ArgumentNullException(nameof(cacheOptions));
        _diagnosticSink = diagnosticSink;
        _fallbackPolicySignature = CreateFallbackPolicySignature();
        _cultureService.CultureChanged += OnCultureChanged;
    }

    /// <inheritdoc />
    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <inheritdoc />
    public event EventHandler<ProTranslateDiagnosticEventArgs>? DiagnosticReported;

    /// <inheritdoc />
    public CultureInfo CurrentCulture => _cultureService.CurrentCulture;

    /// <inheritdoc />
    public CultureInfo CurrentUICulture => _cultureService.CurrentUICulture;

    /// <inheritdoc />
    public LocalizedString this[string key] => GetString(key);

    /// <inheritdoc />
    public int CachedLookupCount => _cache.Count;

    /// <inheritdoc />
    public LocalizedString GetString(string key) => GetString(key, CurrentUICulture);

    /// <inheritdoc />
    public LocalizedString GetString(string key, CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(culture);

        EnsureFallbackPolicyCurrent();

        string policySignature = _fallbackPolicySignature;
        TranslationCacheKey cacheKey = new(key, culture.Name, policySignature);
        if (_cacheOptions.Enabled && _cache.TryGetValue(cacheKey, out LocalizedString cached))
        {
            return cached;
        }

        LocalizedString resolved = ResolveString(key, culture);
        if (ShouldCache(resolved))
        {
            AddCacheEntry(cacheKey, resolved);
        }

        return resolved;
    }

    /// <inheritdoc />
    public void ClearCache() => _cache.Clear();

    /// <inheritdoc />
    public bool RemoveCachedString(string key, CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(culture);

        bool removed = false;
        foreach (TranslationCacheKey cacheKey in _cache.Keys)
        {
            if (string.Equals(cacheKey.Key, key, StringComparison.Ordinal)
                && string.Equals(cacheKey.CultureName, culture.Name, StringComparison.OrdinalIgnoreCase))
            {
                removed |= _cache.TryRemove(cacheKey, out _);
            }
        }

        return removed;
    }

    private LocalizedString ResolveString(string key, CultureInfo culture)
    {
        foreach (CultureInfo candidate in EnumerateFallbackCultures(culture))
        {
            LocalizedString localized;
            try
            {
                localized = _provider.GetString(key, candidate);
            }
            catch (Exception ex)
            {
                ProTranslateDiagnostic diagnostic = CreateProviderFailureDiagnostic(key, candidate, _provider.Name, ex);
                Report(diagnostic);

                if (_options.ProviderFailureBehavior == TranslationProviderFailureBehavior.ReportAndThrow)
                {
                    throw;
                }

                continue;
            }

            Report(localized.Diagnostics);

            if (!localized.ResourceNotFound)
            {
                return localized;
            }
        }

        string value = _options.ReturnKeyWhenMissing ? key : string.Empty;
        ProTranslateDiagnostic missing = CreateMissingTranslationDiagnostic(key, culture, _provider.Name);
        Report(missing);
        return new LocalizedString(key, value, culture, true, _provider.Name)
        {
            Diagnostics = new[] { missing }
        };
    }

    /// <inheritdoc />
    public string Format(string key, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        LocalizedString localized = GetString(key);
        if (arguments.Length == 0)
        {
            return localized.Value;
        }

        try
        {
            return string.Format(CurrentCulture, localized.Value, arguments);
        }
        catch (FormatException ex)
        {
            ProTranslateDiagnostic diagnostic = new(
                ProTranslateDiagnosticKind.FormatFailure,
                ProTranslateDiagnosticSeverity.Error,
                $"Localized format for key '{key}' could not be formatted for culture '{CurrentCulture.Name}'.",
                key,
                CurrentCulture,
                localized.ProviderName,
                ex);

            Report(diagnostic);

            if (_options.FormatFailureBehavior == TranslationFormatFailureBehavior.ReportAndThrow)
            {
                throw;
            }

            return localized.Value;
        }
    }

    /// <inheritdoc />
    public IObservableLocalizedString Observe(string key, params object?[] arguments) =>
        new ObservableLocalizedString(this, key, arguments);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cultureService.CultureChanged -= OnCultureChanged;
        _disposed = true;
    }

    private bool ShouldCache(LocalizedString localized) =>
        _cacheOptions.Enabled && (!localized.ResourceNotFound || _cacheOptions.CacheMissingTranslations);

    private void AddCacheEntry(TranslationCacheKey cacheKey, LocalizedString localized)
    {
        int maximumEntries = _cacheOptions.MaximumEntries;
        if (maximumEntries > 0 && _cache.Count >= maximumEntries)
        {
            ClearCache();
        }

        _cache[cacheKey] = localized;
    }

    private void EnsureFallbackPolicyCurrent()
    {
        string currentSignature = CreateFallbackPolicySignature();
        if (string.Equals(_fallbackPolicySignature, currentSignature, StringComparison.Ordinal))
        {
            return;
        }

        lock (_cachePolicyLock)
        {
            currentSignature = CreateFallbackPolicySignature();
            if (string.Equals(_fallbackPolicySignature, currentSignature, StringComparison.Ordinal))
            {
                return;
            }

            _fallbackPolicySignature = currentSignature;
            ClearCache();
        }
    }

    private string CreateFallbackPolicySignature()
    {
        string fallbackCultures = string.Join(
            ",",
            _options.FallbackCultures.Select(static culture => culture.Name));

        return string.Join(
            "|",
            _provider.Name,
            _options.DefaultCulture.Name,
            fallbackCultures,
            _options.UseParentCultures,
            _options.UseDefaultCulture,
            _options.ReturnKeyWhenMissing,
            _options.ProviderFailureBehavior);
    }

    private IEnumerable<CultureInfo> EnumerateFallbackCultures(CultureInfo requestedCulture)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (CultureInfo culture in EnumerateCultureAndParents(requestedCulture))
        {
            if (seen.Add(culture.Name))
            {
                yield return culture;
            }
        }

        foreach (CultureInfo fallbackCulture in _options.FallbackCultures)
        {
            foreach (CultureInfo culture in EnumerateCultureAndParents(fallbackCulture))
            {
                if (seen.Add(culture.Name))
                {
                    yield return culture;
                }
            }
        }

        if (_options.UseDefaultCulture)
        {
            foreach (CultureInfo culture in EnumerateCultureAndParents(_options.DefaultCulture))
            {
                if (seen.Add(culture.Name))
                {
                    yield return culture;
                }
            }
        }
    }

    private IEnumerable<CultureInfo> EnumerateCultureAndParents(CultureInfo culture)
    {
        yield return culture;

        if (!_options.UseParentCultures)
        {
            yield break;
        }

        CultureInfo current = culture;
        while (!string.IsNullOrEmpty(current.Parent.Name))
        {
            current = current.Parent;
            yield return current;
        }

        if (current.Parent.Equals(CultureInfo.InvariantCulture))
        {
            yield return CultureInfo.InvariantCulture;
        }
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        if (_cacheOptions.ClearOnCultureChanged)
        {
            ClearCache();
        }

        CultureChanged?.Invoke(this, e);
    }

    private void Report(IEnumerable<ProTranslateDiagnostic> diagnostics)
    {
        foreach (ProTranslateDiagnostic diagnostic in diagnostics)
        {
            Report(diagnostic);
        }
    }

    private void Report(ProTranslateDiagnostic diagnostic)
    {
        _diagnosticSink?.Report(diagnostic);
        DiagnosticReported?.Invoke(this, new ProTranslateDiagnosticEventArgs(diagnostic));
    }

    private static ProTranslateDiagnostic CreateMissingTranslationDiagnostic(
        string key,
        CultureInfo culture,
        string providerName) =>
        new(
            ProTranslateDiagnosticKind.MissingTranslation,
            ProTranslateDiagnosticSeverity.Warning,
            $"Translation key '{key}' was not found for culture '{culture.Name}'.",
            key,
            culture,
            providerName);

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

    private readonly record struct TranslationCacheKey(string Key, string CultureName, string FallbackPolicySignature);
}
