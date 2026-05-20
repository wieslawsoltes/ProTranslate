using System.Text.Json;

#pragma warning disable CA1031
namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Manages backup creation, rotation, and restoration for translation sessions.
/// </summary>
public sealed class BackupService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _backupsDirectory;

    public BackupService()
    {
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProTranslateStudio");
        _backupsDirectory = Path.Combine(appData, "backups");
        Directory.CreateDirectory(_backupsDirectory);
    }

    public string BackupsDirectory => _backupsDirectory;

    /// <summary>
    /// Creates a timestamped backup of the given session.
    /// </summary>
    public async Task<BackupMetadata> CreateBackupAsync(TranslationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        string fileName = $"backup_{timestamp}_{session.TargetCulture}.json";
        string path = Path.Combine(_backupsDirectory, fileName);

        string json = JsonSerializer.Serialize(session, s_jsonOptions);
        await File.WriteAllTextAsync(path, json).ConfigureAwait(false);

        return new BackupMetadata(
            path,
            fileName,
            DateTimeOffset.UtcNow,
            new FileInfo(path).Length,
            session.Entries.Count);
    }

    /// <summary>
    /// Lists all existing backups ordered by newest first.
    /// </summary>
    public Task<IReadOnlyList<BackupMetadata>> ListBackupsAsync()
    {
        if (!Directory.Exists(_backupsDirectory))
        {
            return Task.FromResult<IReadOnlyList<BackupMetadata>>([]);
        }

        var backups = Directory.GetFiles(_backupsDirectory, "backup_*.json")
            .Select(path =>
            {
                var info = new FileInfo(path);
                int entryCount = 0;
                try
                {
                    string content = File.ReadAllText(path);
                    var session = JsonSerializer.Deserialize<TranslationSession>(content, s_jsonOptions);
                    entryCount = session?.Entries.Count ?? 0;
                }
                catch
                {
                    // Ignore corrupt backups in listing.
                }

                return new BackupMetadata(
                    path,
                    info.Name,
                    new DateTimeOffset(info.CreationTimeUtc, TimeSpan.Zero),
                    info.Length,
                    entryCount);
            })
            .OrderByDescending(b => b.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<BackupMetadata>>(backups);
    }

    /// <summary>
    /// Restores a session from a backup file.
    /// </summary>
    public async Task<TranslationSession?> RestoreBackupAsync(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        return JsonSerializer.Deserialize<TranslationSession>(json, s_jsonOptions);
    }

    /// <summary>
    /// Deletes a backup file.
    /// </summary>
    public Task DeleteBackupAsync(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Rotates backups by removing the oldest ones that exceed the maximum count.
    /// </summary>
    public async Task RotateBackupsAsync(int maxCount)
    {
        var backups = await ListBackupsAsync().ConfigureAwait(false);
        if (backups.Count <= maxCount)
        {
            return;
        }

        foreach (var backup in backups.Skip(maxCount))
        {
            await DeleteBackupAsync(backup.FilePath).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Metadata about a backup file.
/// </summary>
public sealed record BackupMetadata(
    string FilePath,
    string FileName,
    DateTimeOffset CreatedAt,
    long FileSize,
    int EntryCount);
#pragma warning restore CA1031
