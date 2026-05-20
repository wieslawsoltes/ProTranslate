using System.Net.Http;

namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Orchestrates AI translation across multiple providers.
/// </summary>
public sealed class AiTranslationManager : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<AiProvider, IAiTranslationService> _services;
    private bool _disposed;

    public AiTranslationManager()
    {
        _httpClient = new HttpClient();
        _services = new Dictionary<AiProvider, IAiTranslationService>
        {
            [AiProvider.OpenAI] = new OpenAiTranslationService(_httpClient),
            [AiProvider.Claude] = new ClaudeTranslationService(_httpClient),
            [AiProvider.Gemini] = new GeminiTranslationService(_httpClient)
        };
    }

    /// <summary>
    /// Updates all provider configurations from settings.
    /// </summary>
    public void ApplySettings(StudioSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        foreach (var providerConfig in settings.AiProviders)
        {
            if (_services.TryGetValue(providerConfig.Provider, out var service))
            {
                service.Configure(providerConfig);
            }
        }
    }

    /// <summary>
    /// Gets all providers that have been configured with an API key.
    /// </summary>
    public IReadOnlyList<IAiTranslationService> GetConfiguredProviders()
    {
        return _services.Values.Where(s => s.IsConfigured).ToList();
    }

    /// <summary>
    /// Gets all available providers regardless of configuration.
    /// </summary>
    public IReadOnlyList<IAiTranslationService> GetAllProviders()
    {
        return _services.Values.ToList();
    }

    /// <summary>
    /// Gets a specific provider service.
    /// </summary>
    public IAiTranslationService GetProvider(AiProvider provider)
    {
        return _services[provider];
    }

    /// <summary>
    /// Translates using a specific provider.
    /// </summary>
    public Task<AiTranslationResult> TranslateWithProviderAsync(
        AiProvider provider,
        AiTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        return _services[provider].TranslateAsync(request, cancellationToken);
    }

    /// <summary>
    /// Translates using the first available configured provider.
    /// </summary>
    public async Task<AiTranslationResult> TranslateWithBestAvailableAsync(
        AiTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        var configured = GetConfiguredProviders();
        if (configured.Count == 0)
        {
            return new AiTranslationResult
            {
                ErrorMessage = "No AI provider is configured. Please add an API key in Settings."
            };
        }

        return await configured[0].TranslateAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests connection for a specific provider.
    /// </summary>
    public Task<bool> TestConnectionAsync(
        AiProvider provider,
        CancellationToken cancellationToken = default)
    {
        return _services[provider].TestConnectionAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
