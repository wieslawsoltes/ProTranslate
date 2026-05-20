namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Result returned from an AI translation service.
/// </summary>
public sealed class AiTranslationResult
{
    public List<AiTranslationResultEntry> Entries { get; set; } = [];

    public AiProvider Provider { get; set; }

    public string ModelUsed { get; set; } = string.Empty;

    public int TokensUsed { get; set; }

    public long DurationMs { get; set; }

    public bool IsSuccess { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// A single translated entry within an AI translation result.
/// </summary>
public sealed record AiTranslationResultEntry(
    string Key,
    string TranslatedText,
    double Confidence = 1.0);
