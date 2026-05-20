using System.Text.Json;

#pragma warning disable CA1031
namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Manages translation session persistence, auto-save, and crash recovery.
/// </summary>
public sealed class SessionService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _sessionsDirectory;
    private readonly string _recoveryPath;
    private System.Threading.Timer? _autoSaveTimer;
    private Func<TranslationSession>? _sessionProvider;

    public SessionService()
    {
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProTranslateStudio");
        _sessionsDirectory = Path.Combine(appData, "sessions");
        _recoveryPath = Path.Combine(appData, "recovery.json");
        Directory.CreateDirectory(_sessionsDirectory);
    }

    public string SessionsDirectory => _sessionsDirectory;

    /// <summary>
    /// Saves a session to a specific file path.
    /// </summary>
    public async Task SaveSessionAsync(TranslationSession session, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        session.LastModifiedAt = DateTimeOffset.UtcNow;
        session.IsDirty = false;
        string targetPath = path ?? GetDefaultSessionPath(session);
        string directory = Path.GetDirectoryName(targetPath)!;
        Directory.CreateDirectory(directory);
        string json = JsonSerializer.Serialize(session, s_jsonOptions);
        await File.WriteAllTextAsync(targetPath, json).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a session from a file path.
    /// </summary>
    public async Task<TranslationSession?> LoadSessionAsync(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        return JsonSerializer.Deserialize<TranslationSession>(json, s_jsonOptions);
    }

    /// <summary>
    /// Saves the current session state as a recovery file for crash recovery.
    /// </summary>
    public async Task SaveRecoveryAsync(TranslationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        session.LastModifiedAt = DateTimeOffset.UtcNow;
        string json = JsonSerializer.Serialize(session, s_jsonOptions);
        await File.WriteAllTextAsync(_recoveryPath, json).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if a recovery session exists and returns it.
    /// </summary>
    public async Task<TranslationSession?> GetRecoverySessionAsync()
    {
        if (!File.Exists(_recoveryPath))
        {
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(_recoveryPath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<TranslationSession>(json, s_jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Removes the recovery file after a successful restore or discard.
    /// </summary>
    public void ClearRecoverySession()
    {
        if (File.Exists(_recoveryPath))
        {
            File.Delete(_recoveryPath);
        }
    }

    /// <summary>
    /// Starts the auto-save timer that periodically saves a recovery snapshot.
    /// </summary>
    public void StartAutoSave(TimeSpan interval, Func<TranslationSession> sessionProvider)
    {
        _sessionProvider = sessionProvider;
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = new System.Threading.Timer(
            AutoSaveCallback,
            null,
            interval,
            interval);
    }

    /// <summary>
    /// Stops the auto-save timer.
    /// </summary>
    public void StopAutoSave()
    {
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = null;
        _sessionProvider = null;
    }

    public string GetDefaultSessionPath(TranslationSession session)
    {
        return Path.Combine(_sessionsDirectory, $"{session.SessionId}.json");
    }


    private async void AutoSaveCallback(object? state)
    {
        if (_sessionProvider is null)
        {
            return;
        }

        try
        {
            var session = _sessionProvider();
            if (session.IsDirty)
            {
                await SaveRecoveryAsync(session).ConfigureAwait(false);
            }
        }
        catch
        {
            // Auto-save should never crash the app.
        }
    }
}
#pragma warning restore CA1031
