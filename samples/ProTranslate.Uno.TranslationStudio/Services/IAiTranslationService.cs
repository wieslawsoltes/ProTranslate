namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Interface for AI-powered translation service providers.
/// </summary>
public interface IAiTranslationService
{
    /// <summary>
    /// Gets the provider type.
    /// </summary>
    AiProvider Provider { get; }

    /// <summary>
    /// Gets the human-readable provider name.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets whether this provider is configured with a valid API key.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Translates a batch of entries.
    /// </summary>
    Task<AiTranslationResult> TranslateAsync(
        AiTranslationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Translates a single text string.
    /// </summary>
    Task<string> TranslateSingleAsync(
        string sourceText,
        string sourceLanguage,
        string targetLanguage,
        string context = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection by sending a minimal translation request.
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the provider configuration.
    /// </summary>
    void Configure(AiProviderConfig config);
    
    /// <summary>
    /// Dynamically lists available models from the provider's API.
    /// </summary>
    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default);
}

