using System.Text.Json;

#pragma warning disable CA1031
namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Manages loading and saving application settings to a JSON file in the app data directory.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsPath;

    public SettingsService()
    {
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProTranslateStudio");
        Directory.CreateDirectory(appData);
        _settingsPath = Path.Combine(appData, "settings.json");
    }

    public string SettingsPath => _settingsPath;

    public async Task<StudioSettings> LoadAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = new StudioSettings();
            await SaveAsync(defaults).ConfigureAwait(false);
            return defaults;
        }

        try
        {
            string json = await File.ReadAllTextAsync(_settingsPath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<StudioSettings>(json, s_jsonOptions) ?? new StudioSettings();
        }
        catch
        {
            return new StudioSettings();
        }
    }

    public async Task SaveAsync(StudioSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        string json = JsonSerializer.Serialize(settings, s_jsonOptions);
        string directory = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(_settingsPath, json).ConfigureAwait(false);
    }
}
#pragma warning restore CA1031
