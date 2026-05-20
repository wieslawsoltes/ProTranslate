using System.Text.Json.Serialization;

namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Represents a complete translation session that can be persisted, auto-saved, and recovered.
/// </summary>
public sealed class TranslationSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    public string SourceCulture { get; set; } = "en-US";

    public string TargetCulture { get; set; } = "pl-PL";

    public string FormatName { get; set; } = "XLIFF 2.1";

    public string FileName { get; set; } = string.Empty;

    public List<TranslationSessionEntry> Entries { get; set; } = [];

    [JsonIgnore]
    public bool IsDirty { get; set; }
}
