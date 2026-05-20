namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Application settings persisted as JSON in the app data directory.
/// </summary>
public sealed class StudioSettings
{
    public bool AutoSaveEnabled { get; set; } = true;

    public int AutoSaveIntervalSeconds { get; set; } = 30;

    public bool AutoBackupEnabled { get; set; } = true;

    public int MaxBackupCount { get; set; } = 10;

    public string DefaultExportFormat { get; set; } = "Gettext PO/POT";

    public string LastSessionPath { get; set; } = string.Empty;

    public string AiTranslationContext { get; set; } = string.Empty;

    public List<AiProviderConfig> AiProviders { get; set; } =
    [
        new() { Provider = AiProvider.OpenAI, ModelId = AiProviderConfig.GetDefaultModel(AiProvider.OpenAI), BaseUrl = AiProviderConfig.GetDefaultBaseUrl(AiProvider.OpenAI) },
        new() { Provider = AiProvider.Claude, ModelId = AiProviderConfig.GetDefaultModel(AiProvider.Claude), BaseUrl = AiProviderConfig.GetDefaultBaseUrl(AiProvider.Claude) },
        new() { Provider = AiProvider.Gemini, ModelId = AiProviderConfig.GetDefaultModel(AiProvider.Gemini), BaseUrl = AiProviderConfig.GetDefaultBaseUrl(AiProvider.Gemini) }
    ];
}
