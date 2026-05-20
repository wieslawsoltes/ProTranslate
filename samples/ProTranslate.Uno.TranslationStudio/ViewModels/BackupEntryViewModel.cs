namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Lightweight view model for backup list items in the sidebar.
/// </summary>
public sealed class BackupEntryViewModel : ObservableObject
{
    private readonly BackupService _backupService;

    public BackupEntryViewModel(BackupMetadata metadata, BackupService backupService)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));

        FilePath = metadata.FilePath;
        FileName = metadata.FileName;
        CreatedAt = metadata.CreatedAt;
        FileSize = metadata.FileSize;
        EntryCount = metadata.EntryCount;
    }

    public string FilePath { get; }

    public string FileName { get; }

    public DateTimeOffset CreatedAt { get; }

    public long FileSize { get; }

    public int EntryCount { get; }

    public string CreatedAtLabel => CreatedAt.LocalDateTime.ToString("g");

    public string FileSizeLabel => FileSize < 1024
        ? $"{FileSize} B"
        : FileSize < 1024 * 1024
            ? $"{FileSize / 1024.0:N1} KB"
            : $"{FileSize / (1024.0 * 1024.0):N1} MB";

    public string SummaryLabel => $"{EntryCount} entries · {FileSizeLabel}";
}
