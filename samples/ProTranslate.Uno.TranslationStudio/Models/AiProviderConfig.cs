namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Per-provider AI translation configuration.
/// </summary>
public sealed class AiProviderConfig
{
    public AiProvider Provider { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public int MaxTokens { get; set; } = 4096;

    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Returns the default model ID for the given provider.
    /// </summary>
    public static string GetDefaultModel(AiProvider provider) => provider switch
    {
        AiProvider.OpenAI => "gpt-4o-mini",
        AiProvider.Claude => "claude-3-5-sonnet-20241022",
        AiProvider.Gemini => "gemini-2.5-flash",
        _ => string.Empty
    };

    /// <summary>
    /// Returns the default base URL for the given provider.
    /// </summary>
    public static string GetDefaultBaseUrl(AiProvider provider) => provider switch
    {
        AiProvider.OpenAI => "https://api.openai.com",
        AiProvider.Claude => "https://api.anthropic.com",
        AiProvider.Gemini => "https://generativelanguage.googleapis.com",
        _ => string.Empty
    };

    /// <summary>
    /// Returns available model choices for the given provider.
    /// </summary>
    public static IReadOnlyList<string> GetAvailableModels(AiProvider provider) => provider switch
    {
        AiProvider.OpenAI => [
            "gpt-4o",
            "gpt-4o-mini",
            "gpt-4.5",
            "o1",
            "o1-mini",
            "o1-preview",
            "o3-mini",
            "gpt-4-turbo",
            "gpt-4"
        ],
        AiProvider.Claude => [
            "claude-3-7-sonnet-20250219",
            "claude-3-5-sonnet-20241022",
            "claude-3-5-haiku-20241022",
            "claude-3-opus-20240229",
            "claude-3-sonnet-20240229",
            "claude-3-haiku-20240307"
        ],
        AiProvider.Gemini => [
            "gemini-2.5-flash",
            "gemini-2.5-pro",
            "gemini-2.0-flash",
            "gemini-2.0-flash-lite-preview-02-05",
            "gemini-2.0-pro-exp-02-05",
            "gemini-1.5-flash",
            "gemini-1.5-pro"
        ],
        _ => []
    };
}
