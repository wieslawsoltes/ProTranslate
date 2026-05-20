using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// View model for the settings panel managing AI providers, auto-save, and backup configuration.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly AiTranslationManager _aiManager;
    private StudioSettings _settings;

    private bool _isVisible;
    private string _testResultMessage = string.Empty;
    private bool _isTesting;

    public SettingsViewModel(SettingsService settingsService, AiTranslationManager aiManager, StudioSettings settings)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _aiManager = aiManager ?? throw new ArgumentNullException(nameof(aiManager));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        ProviderConfigs = new ObservableCollection<AiProviderConfigViewModel>(
            _settings.AiProviders.Select(c => new AiProviderConfigViewModel(c, _aiManager)));

        SaveCommand = new RelayCommand(_ => SaveSettings());
        CancelCommand = new RelayCommand(_ => Cancel());
        TestConnectionCommand = new RelayCommand(async p => await TestConnectionAsync(p).ConfigureAwait(false), _ => !IsTesting);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public bool AutoSaveEnabled
    {
        get => _settings.AutoSaveEnabled;
        set
        {
            _settings.AutoSaveEnabled = value;
            OnPropertyChanged();
        }
    }

    public int AutoSaveIntervalSeconds
    {
        get => _settings.AutoSaveIntervalSeconds;
        set
        {
            _settings.AutoSaveIntervalSeconds = Math.Clamp(value, 10, 300);
            OnPropertyChanged();
        }
    }

    public bool AutoBackupEnabled
    {
        get => _settings.AutoBackupEnabled;
        set
        {
            _settings.AutoBackupEnabled = value;
            OnPropertyChanged();
        }
    }

    public int MaxBackupCount
    {
        get => _settings.MaxBackupCount;
        set
        {
            _settings.MaxBackupCount = Math.Clamp(value, 1, 50);
            OnPropertyChanged();
        }
    }

    public string AiTranslationContext
    {
        get => _settings.AiTranslationContext;
        set
        {
            _settings.AiTranslationContext = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<AiProviderConfigViewModel> ProviderConfigs { get; }

    public string TestResultMessage
    {
        get => _testResultMessage;
        set => SetProperty(ref _testResultMessage, value);
    }

    public bool IsTesting
    {
        get => _isTesting;
        set => SetProperty(ref _isTesting, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand TestConnectionCommand { get; }

    /// <summary>
    /// Applies updated settings from the UI back to the underlying settings model.
    /// </summary>
    public void ApplyFromProviderConfigs()
    {
        _settings.AiProviders = ProviderConfigs.Select(vm => vm.ToConfig()).ToList();
    }

    /// <summary>
    /// Gets the current settings snapshot.
    /// </summary>
    public StudioSettings GetSettings() => _settings;

    /// <summary>
    /// Replaces the current settings (e.g., after loading from disk).
    /// </summary>
    public void LoadSettings(StudioSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ProviderConfigs.Clear();
        foreach (var config in _settings.AiProviders)
        {
            ProviderConfigs.Add(new AiProviderConfigViewModel(config, _aiManager));
        }

        OnPropertyChanged(nameof(AutoSaveEnabled));
        OnPropertyChanged(nameof(AutoSaveIntervalSeconds));
        OnPropertyChanged(nameof(AutoBackupEnabled));
        OnPropertyChanged(nameof(MaxBackupCount));
        OnPropertyChanged(nameof(AiTranslationContext));
    }

#pragma warning disable CA1031
    private async void SaveSettings()
    {
        ApplyFromProviderConfigs();
        _aiManager.ApplySettings(_settings);
        try
        {
            await _settingsService.SaveAsync(_settings).ConfigureAwait(false);
        }
        catch
        {
            // Settings save failure is non-fatal.
        }

        IsVisible = false;
    }
#pragma warning restore CA1031

    private void Cancel()
    {
        IsVisible = false;
    }

#pragma warning disable CA1031
    private async Task TestConnectionAsync(object? parameter)
    {
        if (parameter is not AiProviderConfigViewModel providerVm)
        {
            return;
        }

        IsTesting = true;
        TestResultMessage = $"Testing {providerVm.ProviderName}...";

        try
        {
            // Apply the current config temporarily for testing
            var testConfig = providerVm.ToConfig();
            var provider = _aiManager.GetProvider(testConfig.Provider);
            provider.Configure(testConfig);

            bool success = await _aiManager.TestConnectionAsync(testConfig.Provider).ConfigureAwait(false);
            TestResultMessage = success
                ? $"✓ {providerVm.ProviderName} connection successful!"
                : $"✗ {providerVm.ProviderName} connection failed.";
        }
        catch (Exception ex)
        {
            TestResultMessage = $"✗ {providerVm.ProviderName} error: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }
#pragma warning restore CA1031
}

/// <summary>
/// View model wrapping a single AI provider configuration for editing in the settings UI.
/// </summary>
public sealed class AiProviderConfigViewModel : ObservableObject
{
    private readonly AiTranslationManager? _aiManager;
    private bool _isEnabled;
    private string _apiKey;
    private string _modelId;
    private string _baseUrl;
    private int _maxTokens;
    private double _temperature;
    private bool _isDiscovering;
    private string _statusMessage = string.Empty;
    private ObservableCollection<string> _availableModels;

    public AiProviderConfigViewModel(AiProviderConfig config, AiTranslationManager? aiManager = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        _aiManager = aiManager;
        Provider = config.Provider;
        _isEnabled = config.IsEnabled;
        _apiKey = config.ApiKey;
        _modelId = string.IsNullOrWhiteSpace(config.ModelId) ? AiProviderConfig.GetDefaultModel(config.Provider) : config.ModelId;
        _baseUrl = string.IsNullOrWhiteSpace(config.BaseUrl) ? AiProviderConfig.GetDefaultBaseUrl(config.Provider) : config.BaseUrl;
        _maxTokens = config.MaxTokens;
        _temperature = config.Temperature;
        
        _availableModels = new ObservableCollection<string>(AiProviderConfig.GetAvailableModels(Provider));
        DiscoverModelsCommand = new RelayCommand(async _ => await DiscoverModelsAsync().ConfigureAwait(false), _ => !IsDiscovering);
    }

    public AiProvider Provider { get; }

    public string ProviderName => Provider switch
    {
        AiProvider.OpenAI => "OpenAI",
        AiProvider.Claude => "Anthropic Claude",
        AiProvider.Gemini => "Google Gemini",
        _ => Provider.ToString()
    };

    public string ProviderIcon => Provider switch
    {
        AiProvider.OpenAI => "\uE945",
        AiProvider.Claude => "\uE8D4",
        AiProvider.Gemini => "\uE771",
        _ => "\uE946"
    };

    public ObservableCollection<string> AvailableModels
    {
        get => _availableModels;
        set => SetProperty(ref _availableModels, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    public string ModelId
    {
        get => _modelId;
        set => SetProperty(ref _modelId, value);
    }

    public string BaseUrl
    {
        get => _baseUrl;
        set => SetProperty(ref _baseUrl, value);
    }

    public int MaxTokens
    {
        get => _maxTokens;
        set => SetProperty(ref _maxTokens, value);
    }

    public double Temperature
    {
        get => _temperature;
        set => SetProperty(ref _temperature, value);
    }

    public bool IsDiscovering
    {
        get => _isDiscovering;
        set => SetProperty(ref _isDiscovering, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand DiscoverModelsCommand { get; }

#pragma warning disable CA1031
    private async Task DiscoverModelsAsync()
    {
        if (_aiManager == null)
        {
            return;
        }

        App.MainWindow?.DispatcherQueue?.TryEnqueue(() =>
        {
            IsDiscovering = true;
            StatusMessage = "Discovering...";
        });

        try
        {
            // Create a temporary configuration with current UI values for model discovery
            var tempConfig = ToConfig();
            var service = _aiManager.GetProvider(Provider);
            service.Configure(tempConfig);

            var list = await service.ListModelsAsync().ConfigureAwait(false);

            App.MainWindow?.DispatcherQueue?.TryEnqueue(() =>
            {
                AvailableModels.Clear();
                foreach (var model in list)
                {
                    AvailableModels.Add(model);
                }

                // If currently selected ModelId is no longer in the list (or wasn't there), and we got models back,
                // keep the current one or set it to first if current is empty
                if (list.Count > 0 && !list.Contains(ModelId))
                {
                    // If it is in the hardcoded list but not active, just keep it, otherwise select first or default
                    if (string.IsNullOrWhiteSpace(ModelId))
                    {
                        ModelId = list[0];
                    }
                }

                StatusMessage = $"✓ Discovered {list.Count} models";
            });
        }
        catch (Exception ex)
        {
            App.MainWindow?.DispatcherQueue?.TryEnqueue(() =>
            {
                StatusMessage = $"✗ Discovery failed: {ex.Message}";
            });
        }
        finally
        {
            App.MainWindow?.DispatcherQueue?.TryEnqueue(() =>
            {
                IsDiscovering = false;
            });
        }
    }
#pragma warning restore CA1031

    public AiProviderConfig ToConfig() => new()
    {
        Provider = Provider,
        IsEnabled = IsEnabled,
        ApiKey = ApiKey,
        ModelId = ModelId,
        BaseUrl = BaseUrl,
        MaxTokens = MaxTokens,
        Temperature = Temperature
    };
}
