using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using ProTranslate;
using ProTranslate.Formats;
using ProTranslate.Generated;

namespace ProTranslate.Uno.TranslationStudio;

public sealed class TranslationStudioViewModel : ObservableObject, IDisposable
{
    private readonly ITranslationCatalogGateway _catalogGateway;
    private readonly CultureService _cultureService;
    private readonly global::ProTranslate.TranslationService _translationService;

    // Services
    private readonly SessionService _sessionService;
    private readonly BackupService _backupService;
    private readonly SettingsService _settingsService;
    private readonly AiTranslationManager _aiManager;

    // Commands
    private readonly RelayCommand _approveSelectedCommand;
    private readonly RelayCommand _markReviewCommand;
    private readonly RelayCommand _exportCatalogCommand;
    private readonly RelayCommand _addKeyCommand;
    private readonly RelayCommand _deleteKeyCommand;
    private readonly RelayCommand _autoTranslateSelectedCommand;
    private readonly RelayCommand _autoTranslateAllCommand;
    private readonly RelayCommand _addCultureCommand;
    private readonly RelayCommand _saveSessionCommand;
    private readonly RelayCommand _undoCommand;
    private readonly RelayCommand _redoCommand;
    private readonly RelayCommand _createBackupCommand;
    private readonly RelayCommand _aiTranslateSelectedCommand;
    private readonly RelayCommand _aiTranslateAllMissingCommand;
    private readonly RelayCommand _acceptAiSuggestionCommand;
    private readonly RelayCommand _rejectAiSuggestionCommand;
    private readonly RelayCommand _importFromFileCommand;
    private readonly RelayCommand _exportToFileCommand;

    // State
    private CultureChoice _selectedUiCulture;
    private CultureChoice _selectedTargetCulture;
    private CatalogFormatChoice _selectedImportFormat;
    private CatalogFormatChoice _selectedExportFormat;
    private CatalogEntryViewModel? _selectedEntry;
    private TranslationCatalogSnapshot _currentSnapshot;
    private string _lastExportSummary;
    private bool _disposed;

    // Search and filtering
    private string _searchText = string.Empty;
    private string _selectedStateFilter = "All";

    // Adding key state
    private string _newKeyName = string.Empty;
    private string _newKeySource = string.Empty;

    // Adding culture state
    private string _newCultureCode = string.Empty;
    private string _newCultureName = string.Empty;

    // Session management
    private TranslationSession _currentSession;
    private bool _isDirty;
    private string _lastSavedLabel = "Not saved";
    private bool _isAutoSaving;

    // Undo/Redo
    private readonly Stack<UndoRedoEntry> _undoStack = new();
    private readonly Stack<UndoRedoEntry> _redoStack = new();
    private const int MaxUndoStackSize = 100;
    private bool _suppressUndoCapture;

    // AI Translation
    private AiProvider _selectedAiProvider = AiProvider.OpenAI;
    private bool _isAiTranslating;
    private double _aiTranslationProgress;
    private string _aiStatusMessage = string.Empty;
    private string _aiSuggestion = string.Empty;
    private string _aiSuggestionProvider = string.Empty;
    private string _aiSuggestionModel = string.Empty;
    private int _aiSuggestionTokens;
    private long _aiSuggestionDurationMs;
    private bool _hasAiSuggestion;

    // Settings
    private SettingsViewModel? _settingsViewModel;
    private StudioSettings _settings;

    public TranslationStudioViewModel()
        : this(new InMemoryTranslationCatalogGateway())
    {
    }

    public TranslationStudioViewModel(ITranslationCatalogGateway catalogGateway)
    {
        _catalogGateway = catalogGateway ?? throw new ArgumentNullException(nameof(catalogGateway));

        // Initialize services
        _sessionService = new SessionService();
        _backupService = new BackupService();
        _settingsService = new SettingsService();
        _aiManager = new AiTranslationManager();
        _settings = new StudioSettings();

        UiCultures =
        [
            new CultureChoice("en-US", "English"),
            new CultureChoice("pl-PL", "Polski")
        ];

        TargetCultures = new ObservableCollection<CultureChoice>(
        [
            new CultureChoice("pl-PL", "Polish"),
            new CultureChoice("de-DE", "German"),
            new CultureChoice("ja-JP", "Japanese")
        ]);

        ImportFormats =
        [
            new CatalogFormatChoice("XLIFF 2.1", ".xlf", "Modern CAT/TMS exchange", TranslationFileFormat.Xliff20),
            new CatalogFormatChoice("XLIFF 1.2", ".xlf", "Legacy CAT/TMS exchange", TranslationFileFormat.Xliff12),
            new CatalogFormatChoice("Gettext PO/POT", ".po", "Open-source gettext catalog", TranslationFileFormat.GettextPo),
            new CatalogFormatChoice("RESX", ".resx", ".NET resource catalog", TranslationFileFormat.Resx),
            new CatalogFormatChoice("Android XML", ".xml", "Mobile resource catalog", TranslationFileFormat.AndroidResources),
            new CatalogFormatChoice("Apple .strings", ".strings", "Apple string resources", TranslationFileFormat.AppleStrings),
            new CatalogFormatChoice("Apple .stringsdict", ".stringsdict", "Apple plural resources", TranslationFileFormat.AppleStringsdict),
            new CatalogFormatChoice("Apple .xcstrings", ".xcstrings", "Xcode string catalog", TranslationFileFormat.AppleXcstrings),
            new CatalogFormatChoice("Flutter ARB", ".arb", "Flutter gen-l10n catalog", TranslationFileFormat.FlutterArb),
            new CatalogFormatChoice("i18next JSON", ".json", "Web localization catalog", TranslationFileFormat.I18NextJson),
            new CatalogFormatChoice("CSV", ".csv", "Spreadsheet review", TranslationFileFormat.Csv),
            new CatalogFormatChoice("TSV", ".tsv", "Tab-delimited review", TranslationFileFormat.Tsv),
            new CatalogFormatChoice("ProTranslate JSON", ".protranslate.json", "Generated provider source", TranslationFileFormat.ProTranslateJson)
        ];

        ExportFormats =
        [
            new CatalogFormatChoice("XLIFF 2.1", ".xlf", "Preferred CAT/TMS handoff", TranslationFileFormat.Xliff20),
            new CatalogFormatChoice("XLIFF 1.2", ".xlf", "Legacy CAT/TMS handoff", TranslationFileFormat.Xliff12),
            new CatalogFormatChoice("Gettext PO/POT", ".po", "Gettext handoff", TranslationFileFormat.GettextPo),
            new CatalogFormatChoice("RESX", ".resx", "ResourceManager pipeline", TranslationFileFormat.Resx),
            new CatalogFormatChoice("Android XML", ".xml", "Android resources", TranslationFileFormat.AndroidResources),
            new CatalogFormatChoice("Apple .strings", ".strings", "Apple resources", TranslationFileFormat.AppleStrings),
            new CatalogFormatChoice("Apple .stringsdict", ".stringsdict", "Apple plural resources", TranslationFileFormat.AppleStringsdict),
            new CatalogFormatChoice("Apple .xcstrings", ".xcstrings", "Xcode string catalog", TranslationFileFormat.AppleXcstrings),
            new CatalogFormatChoice("Flutter ARB", ".arb", "Flutter gen-l10n catalog", TranslationFileFormat.FlutterArb),
            new CatalogFormatChoice("i18next JSON", ".json", "Web localization catalog", TranslationFileFormat.I18NextJson),
            new CatalogFormatChoice("CSV", ".csv", "Spreadsheet review", TranslationFileFormat.Csv),
            new CatalogFormatChoice("TSV", ".tsv", "Tab-delimited review", TranslationFileFormat.Tsv),
            new CatalogFormatChoice("ProTranslate JSON", ".protranslate.json", "Source generator input", TranslationFileFormat.ProTranslateJson)
        ];

        _selectedUiCulture = UiCultures[0];
        _selectedTargetCulture = TargetCultures[0];
        _selectedImportFormat = ImportFormats[0];
        _selectedExportFormat = ExportFormats[2];

        _cultureService = new CultureService(CultureInfo.GetCultureInfo(_selectedUiCulture.CultureName));
        var provider = new ProTranslateGeneratedTranslationProvider("TranslationStudioGeneratedCatalog");
        var options = new TranslationFallbackOptions
        {
            DefaultCulture = CultureInfo.GetCultureInfo("en-US")
        };
        options.FallbackCultures.Add(CultureInfo.GetCultureInfo("en-US"));
        _translationService = new global::ProTranslate.TranslationService(provider, _cultureService, options);
        Strings = new ProTranslateStrings(_translationService);
        Strings.PropertyChanged += OnStringsChanged;

        // Initialize commands
        ImportDemoCommand = new RelayCommand(_ => ImportFromClipboard());
        _exportCatalogCommand = new RelayCommand(_ => ExportCatalog(), _ => Entries.Count > 0);

        _approveSelectedCommand = new RelayCommand(_ => SetSelectedState(TranslationReviewState.Approved), _ => SelectedEntry is not null);
        _markReviewCommand = new RelayCommand(_ => SetSelectedState(TranslationReviewState.Review), _ => SelectedEntry is not null);

        _addKeyCommand = new RelayCommand(_ => AddKey(), _ => !string.IsNullOrWhiteSpace(NewKeyName));
        _deleteKeyCommand = new RelayCommand(_ => DeleteSelectedKey(), _ => SelectedEntry is not null);

        _autoTranslateSelectedCommand = new RelayCommand(_ => AutoTranslateSelected(), _ => SelectedEntry is not null);
        _autoTranslateAllCommand = new RelayCommand(_ => AutoTranslateAll(), _ => Entries.Count > 0);

        _addCultureCommand = new RelayCommand(_ => AddTargetCulture(), _ => !string.IsNullOrWhiteSpace(NewCultureCode) && !string.IsNullOrWhiteSpace(NewCultureName));

        // Session commands
        _saveSessionCommand = new RelayCommand(async _ => await SaveSessionAsync().ConfigureAwait(false));
        _undoCommand = new RelayCommand(_ => Undo(), _ => CanUndo);
        _redoCommand = new RelayCommand(_ => Redo(), _ => CanRedo);

        // Backup commands
        _createBackupCommand = new RelayCommand(async _ => await CreateBackupAsync().ConfigureAwait(false));

        // AI commands
        _aiTranslateSelectedCommand = new RelayCommand(async _ => await AiTranslateSelectedAsync().ConfigureAwait(false), _ => SelectedEntry is not null && !IsAiTranslating);
        _aiTranslateAllMissingCommand = new RelayCommand(async _ => await AiTranslateAllMissingAsync().ConfigureAwait(false), _ => Entries.Count > 0 && !IsAiTranslating);
        _acceptAiSuggestionCommand = new RelayCommand(_ => AcceptAiSuggestion(), _ => HasAiSuggestion && SelectedEntry is not null);
        _rejectAiSuggestionCommand = new RelayCommand(_ => RejectAiSuggestion(), _ => HasAiSuggestion);

        // File import/export commands
        _importFromFileCommand = new RelayCommand(_ => { /* Handled in code-behind with file picker */ });
        _exportToFileCommand = new RelayCommand(_ => { /* Handled in code-behind with file picker */ }, _ => Entries.Count > 0);

        // Assign public command properties
        ExportCatalogCommand = _exportCatalogCommand;
        ApproveSelectedCommand = _approveSelectedCommand;
        MarkReviewCommand = _markReviewCommand;
        AddKeyCommand = _addKeyCommand;
        DeleteKeyCommand = _deleteKeyCommand;
        AutoTranslateSelectedCommand = _autoTranslateSelectedCommand;
        AutoTranslateAllCommand = _autoTranslateAllCommand;
        AddCultureCommand = _addCultureCommand;
        SaveSessionCommand = _saveSessionCommand;
        UndoCommand = _undoCommand;
        RedoCommand = _redoCommand;
        CreateBackupCommand = _createBackupCommand;
        AiTranslateSelectedCommand = _aiTranslateSelectedCommand;
        AiTranslateAllMissingCommand = _aiTranslateAllMissingCommand;
        AcceptAiSuggestionCommand = _acceptAiSuggestionCommand;
        RejectAiSuggestionCommand = _rejectAiSuggestionCommand;
        ImportFromFileCommand = _importFromFileCommand;
        ExportToFileCommand = _exportToFileCommand;

        // Initialize session
        _currentSession = new TranslationSession
        {
            SourceCulture = "en-US",
            TargetCulture = _selectedTargetCulture.CultureName,
            FormatName = _selectedImportFormat.Name
        };

        _currentSnapshot = _catalogGateway.LoadDemoCatalog(_selectedImportFormat, _selectedTargetCulture);
        _lastExportSummary = Strings.ReadyStatus;
        LoadSnapshot(_currentSnapshot);

        // Load settings and start auto-save asynchronously
        _ = InitializeAsync();
    }

    // ==================== Public Properties ====================

    public ProTranslateStrings Strings { get; }

    public IReadOnlyList<CultureChoice> UiCultures { get; }

    public ObservableCollection<CultureChoice> TargetCultures { get; }

    public IReadOnlyList<CatalogFormatChoice> ImportFormats { get; }

    public IReadOnlyList<CatalogFormatChoice> ExportFormats { get; }

    public ObservableCollection<CatalogEntryViewModel> Entries { get; } = [];

    public ObservableCollection<CatalogEntryViewModel> FilteredEntries { get; } = [];

    public ObservableCollection<CultureCoverageViewModel> Coverage { get; } = [];

    public ObservableCollection<BackupEntryViewModel> Backups { get; } = [];

    // ==================== Commands ====================

    public ICommand ImportDemoCommand { get; }
    public ICommand ExportCatalogCommand { get; }
    public ICommand ApproveSelectedCommand { get; }
    public ICommand MarkReviewCommand { get; }
    public ICommand AddKeyCommand { get; }
    public ICommand DeleteKeyCommand { get; }
    public ICommand AutoTranslateSelectedCommand { get; }
    public ICommand AutoTranslateAllCommand { get; }
    public ICommand AddCultureCommand { get; }
    public ICommand SaveSessionCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand CreateBackupCommand { get; }
    public ICommand AiTranslateSelectedCommand { get; }
    public ICommand AiTranslateAllMissingCommand { get; }
    public ICommand AcceptAiSuggestionCommand { get; }
    public ICommand RejectAiSuggestionCommand { get; }
    public ICommand ImportFromFileCommand { get; }
    public ICommand ExportToFileCommand { get; }

    // ==================== Search & Filtering ====================

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                RefreshFilters();
            }
        }
    }

    public string SelectedStateFilter
    {
        get => _selectedStateFilter;
        set
        {
            if (SetProperty(ref _selectedStateFilter, value))
            {
                RefreshFilters();
            }
        }
    }

    // ==================== Key Management ====================

    public string NewKeyName
    {
        get => _newKeyName;
        set
        {
            if (SetProperty(ref _newKeyName, value))
            {
                _addKeyCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewKeySource
    {
        get => _newKeySource;
        set => SetProperty(ref _newKeySource, value);
    }

    // ==================== Culture Management ====================

    public string NewCultureCode
    {
        get => _newCultureCode;
        set
        {
            if (SetProperty(ref _newCultureCode, value))
            {
                _addCultureCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewCultureName
    {
        get => _newCultureName;
        set
        {
            if (SetProperty(ref _newCultureName, value))
            {
                _addCultureCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // ==================== Culture Selection ====================

    public CultureChoice SelectedUiCulture
    {
        get => _selectedUiCulture;
        set
        {
            if (SetProperty(ref _selectedUiCulture, value))
            {
                _cultureService.SetCulture(CultureInfo.GetCultureInfo(value.CultureName));
                RaiseLocalizedText();
            }
        }
    }

    public CultureChoice SelectedTargetCulture
    {
        get => _selectedTargetCulture;
        set
        {
            if (SetProperty(ref _selectedTargetCulture, value))
            {
                LoadDemoCatalog();
            }
        }
    }

    public CatalogFormatChoice SelectedImportFormat
    {
        get => _selectedImportFormat;
        set
        {
            if (SetProperty(ref _selectedImportFormat, value))
            {
                LoadDemoCatalog();
            }
        }
    }

    public CatalogFormatChoice SelectedExportFormat
    {
        get => _selectedExportFormat;
        set => SetProperty(ref _selectedExportFormat, value);
    }

    // ==================== Selected Entry ====================

    public CatalogEntryViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                RaiseSelectedEntryProperties();
                _approveSelectedCommand.RaiseCanExecuteChanged();
                _markReviewCommand.RaiseCanExecuteChanged();
                _deleteKeyCommand.RaiseCanExecuteChanged();
                _autoTranslateSelectedCommand.RaiseCanExecuteChanged();
                _aiTranslateSelectedCommand.RaiseCanExecuteChanged();
                _acceptAiSuggestionCommand.RaiseCanExecuteChanged();
                // Clear AI suggestion when selection changes
                ClearAiSuggestion();
            }
        }
    }

    public string LastExportSummary
    {
        get => _lastExportSummary;
        private set => SetProperty(ref _lastExportSummary, value);
    }

    public string CatalogFileName => _currentSnapshot.FileName;

    public string CulturePair => string.Create(CultureInfo.InvariantCulture, $"{_currentSnapshot.SourceCulture} -> {_currentSnapshot.TargetCulture}");

    public string SourceFormat => _currentSnapshot.SourceFormat;

    public int TotalCount => Entries.Count;

    public int ApprovedCount => Entries.Count(entry => entry.State == TranslationReviewState.Approved);

    public int ReviewCount => Entries.Count(entry => entry.State == TranslationReviewState.Review);

    public int MissingCount => Entries.Count(entry => entry.State == TranslationReviewState.Missing);

    public double CoveragePercent => TotalCount == 0 ? 0d : ApprovedCount * 100d / TotalCount;

    public string CoverageLabel => CoveragePercent.ToString("N0", CultureInfo.CurrentCulture) + "%";

    public string SelectedKey => SelectedEntry?.Key ?? Strings.NoSelection;

    public string SelectedSourceText => SelectedEntry?.SourceText ?? string.Empty;

    public string SelectedTargetText
    {
        get => SelectedEntry?.TargetText ?? string.Empty;
        set
        {
            if (SelectedEntry is null)
            {
                return;
            }

            string oldValue = SelectedEntry.TargetText;
            if (oldValue != value)
            {
                CaptureUndo(SelectedEntry.Key, nameof(CatalogEntryViewModel.TargetText), oldValue, value);
                SelectedEntry.TargetText = value;
                MarkDirty();
                RaiseMetrics();
            }
        }
    }

    public string SelectedNotes
    {
        get => SelectedEntry?.Notes ?? string.Empty;
        set
        {
            if (SelectedEntry is not null)
            {
                SelectedEntry.Notes = value;
                MarkDirty();
            }
        }
    }

    public string SelectedStateLabel => SelectedEntry?.StateLabel ?? Strings.NoSelection;

    public string SelectedDiagnostics => SelectedEntry?.Diagnostics ?? Strings.NoDiagnostics;

    public bool SelectedEntryHasDiagnostics => SelectedEntry?.HasDiagnostics ?? false;

    public TranslationReviewState SelectedEntryState => SelectedEntry?.State ?? TranslationReviewState.Missing;

    // ==================== Session Properties ====================

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
            {
                OnPropertyChanged(nameof(DirtyIndicator));
            }
        }
    }

    public string DirtyIndicator => IsDirty ? "●" : "";

    public string LastSavedLabel
    {
        get => _lastSavedLabel;
        private set => SetProperty(ref _lastSavedLabel, value);
    }

    public bool IsAutoSaving
    {
        get => _isAutoSaving;
        private set => SetProperty(ref _isAutoSaving, value);
    }

    // ==================== Undo/Redo Properties ====================

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public string UndoLabel => CanUndo ? $"Undo ({_undoStack.Count})" : "Undo";

    public string RedoLabel => CanRedo ? $"Redo ({_redoStack.Count})" : "Redo";

    // ==================== AI Translation Properties ====================

    public AiProvider SelectedAiProvider
    {
        get => _selectedAiProvider;
        set => SetProperty(ref _selectedAiProvider, value);
    }

    public IReadOnlyList<AiProvider> AvailableAiProviders { get; } = [AiProvider.OpenAI, AiProvider.Claude, AiProvider.Gemini];

    public bool IsAiTranslating
    {
        get => _isAiTranslating;
        private set
        {
            if (SetProperty(ref _isAiTranslating, value))
            {
                _aiTranslateSelectedCommand.RaiseCanExecuteChanged();
                _aiTranslateAllMissingCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public double AiTranslationProgress
    {
        get => _aiTranslationProgress;
        private set => SetProperty(ref _aiTranslationProgress, value);
    }

    public string AiStatusMessage
    {
        get => _aiStatusMessage;
        private set => SetProperty(ref _aiStatusMessage, value);
    }

    public string AiSuggestion
    {
        get => _aiSuggestion;
        private set => SetProperty(ref _aiSuggestion, value);
    }

    public string AiSuggestionProvider
    {
        get => _aiSuggestionProvider;
        private set => SetProperty(ref _aiSuggestionProvider, value);
    }

    public string AiSuggestionModel
    {
        get => _aiSuggestionModel;
        private set => SetProperty(ref _aiSuggestionModel, value);
    }

    public int AiSuggestionTokens
    {
        get => _aiSuggestionTokens;
        private set => SetProperty(ref _aiSuggestionTokens, value);
    }

    public long AiSuggestionDurationMs
    {
        get => _aiSuggestionDurationMs;
        private set => SetProperty(ref _aiSuggestionDurationMs, value);
    }

    public bool HasAiSuggestion
    {
        get => _hasAiSuggestion;
        private set
        {
            if (SetProperty(ref _hasAiSuggestion, value))
            {
                _acceptAiSuggestionCommand.RaiseCanExecuteChanged();
                _rejectAiSuggestionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // ==================== Settings ====================

    public SettingsViewModel? SettingsVM
    {
        get => _settingsViewModel;
        private set => SetProperty(ref _settingsViewModel, value);
    }

    public AiTranslationManager AiManager => _aiManager;

    public SessionService SessionServiceInstance => _sessionService;

    public BackupService BackupServiceInstance => _backupService;

    public ITranslationCatalogGateway CatalogGateway => _catalogGateway;

    // ==================== Initialization ====================

#pragma warning disable CA1031
    private async Task InitializeAsync()
    {
        try
        {
            _settings = await _settingsService.LoadAsync().ConfigureAwait(false);
            _aiManager.ApplySettings(_settings);

            SettingsVM = new SettingsViewModel(_settingsService, _aiManager, _settings);

            if (_settings.AutoSaveEnabled)
            {
                StartAutoSave();
            }

            await RefreshBackupsAsync().ConfigureAwait(false);
        }
        catch
        {
            _settings = new StudioSettings();
            SettingsVM = new SettingsViewModel(_settingsService, _aiManager, _settings);
        }
    }
#pragma warning restore CA1031

    // ==================== Session Management ====================

    public TranslationSession CreateSessionSnapshot()
    {
        _currentSession.LastModifiedAt = DateTimeOffset.UtcNow;
        _currentSession.TargetCulture = SelectedTargetCulture.CultureName;
        _currentSession.FormatName = SelectedImportFormat.Name;
        _currentSession.FileName = CatalogFileName;
        _currentSession.IsDirty = IsDirty;
        _currentSession.Entries = Entries.Select(e => new TranslationSessionEntry(
            e.Key,
            e.SourceText,
            e.TargetText,
            e.State.ToString(),
            e.Notes,
            e.Diagnostics)).ToList();
        return _currentSession;
    }

    public void RestoreFromSession(TranslationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _currentSession = session;

        DetachEntryHandlers();
        Entries.Clear();

        foreach (var entry in session.Entries)
        {
            var state = Enum.TryParse<TranslationReviewState>(entry.State, out var s) ? s : TranslationReviewState.Missing;
            var catalogEntry = new TranslationCatalogEntry(
                entry.Key,
                entry.SourceText,
                entry.TargetText,
                state,
                entry.Notes,
                entry.Diagnostics);
            var vm = new CatalogEntryViewModel(catalogEntry);
            vm.PropertyChanged += OnEntryChanged;
            Entries.Add(vm);
        }

        RefreshFilters();
        RaiseCatalogProperties();
        _exportCatalogCommand.RaiseCanExecuteChanged();
        _autoTranslateAllCommand.RaiseCanExecuteChanged();
        _aiTranslateAllMissingCommand.RaiseCanExecuteChanged();
        IsDirty = false;
        LastSavedLabel = $"Restored {session.LastModifiedAt.LocalDateTime:g}";
    }

#pragma warning disable CA1031
    public async Task SaveSessionAsync()
    {
        try
        {
            var snapshot = CreateSessionSnapshot();
            await _sessionService.SaveSessionAsync(snapshot).ConfigureAwait(false);
            IsDirty = false;
            LastSavedLabel = $"Saved {DateTimeOffset.Now.LocalDateTime:T}";
            LastExportSummary = "Session saved successfully.";

            if (_settings.AutoBackupEnabled)
            {
                await _backupService.CreateBackupAsync(snapshot).ConfigureAwait(false);
                await _backupService.RotateBackupsAsync(_settings.MaxBackupCount).ConfigureAwait(false);
                await RefreshBackupsAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LastExportSummary = $"Save failed: {ex.Message}";
        }
    }

    public async Task<TranslationSession?> CheckRecoveryAsync()
    {
        try
        {
            return await _sessionService.GetRecoverySessionAsync().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public void ClearRecovery()
    {
        _sessionService.ClearRecoverySession();
    }
#pragma warning restore CA1031

    private void MarkDirty()
    {
        IsDirty = true;
        _currentSession.IsDirty = true;
    }

    private void StartAutoSave()
    {
        var interval = TimeSpan.FromSeconds(Math.Max(10, _settings.AutoSaveIntervalSeconds));
        _sessionService.StartAutoSave(interval, () =>
        {
            IsAutoSaving = true;
            var session = CreateSessionSnapshot();
            IsAutoSaving = false;
            return session;
        });
    }

    // ==================== Undo/Redo ====================

    private void CaptureUndo(string key, string propertyName, string oldValue, string newValue)
    {
        if (_suppressUndoCapture)
        {
            return;
        }

        if (_undoStack.Count >= MaxUndoStackSize)
        {
            // Convert to list, remove oldest, convert back — bounded stack
            var list = new List<UndoRedoEntry>(_undoStack.Reverse());
            if (list.Count > 0)
            {
                list.RemoveAt(0);
            }
            _undoStack.Clear();
            foreach (var item in list)
            {
                _undoStack.Push(item);
            }
        }

        _undoStack.Push(new UndoRedoEntry(key, propertyName, oldValue, newValue, DateTimeOffset.UtcNow));
        _redoStack.Clear();
        RaiseUndoRedoProperties();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        var entry = _undoStack.Pop();
        _redoStack.Push(entry);

        _suppressUndoCapture = true;
        ApplyUndoRedoValue(entry.Key, entry.PropertyName, entry.OldValue);
        _suppressUndoCapture = false;

        RaiseUndoRedoProperties();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        var entry = _redoStack.Pop();
        _undoStack.Push(entry);

        _suppressUndoCapture = true;
        ApplyUndoRedoValue(entry.Key, entry.PropertyName, entry.NewValue);
        _suppressUndoCapture = false;

        RaiseUndoRedoProperties();
    }

    private void ApplyUndoRedoValue(string key, string propertyName, string value)
    {
        var entry = Entries.FirstOrDefault(e => e.Key == key);
        if (entry is null)
        {
            return;
        }

        switch (propertyName)
        {
            case nameof(CatalogEntryViewModel.TargetText):
                entry.TargetText = value;
                break;
            case nameof(CatalogEntryViewModel.State):
                if (Enum.TryParse<TranslationReviewState>(value, out var state))
                {
                    entry.State = state;
                }
                break;
        }

        if (SelectedEntry?.Key == key)
        {
            RaiseSelectedEntryProperties();
        }

        RaiseMetrics();
        MarkDirty();
    }

    private void RaiseUndoRedoProperties()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoLabel));
        OnPropertyChanged(nameof(RedoLabel));
        _undoCommand.RaiseCanExecuteChanged();
        _redoCommand.RaiseCanExecuteChanged();
    }

    // ==================== Backup Management ====================

#pragma warning disable CA1031
    public async Task CreateBackupAsync()
    {
        try
        {
            var session = CreateSessionSnapshot();
            await _backupService.CreateBackupAsync(session).ConfigureAwait(false);
            await _backupService.RotateBackupsAsync(_settings.MaxBackupCount).ConfigureAwait(false);
            await RefreshBackupsAsync().ConfigureAwait(false);
            LastExportSummary = "Backup created successfully.";
        }
        catch (Exception ex)
        {
            LastExportSummary = $"Backup failed: {ex.Message}";
        }
    }

    public async Task RestoreBackupAsync(string path)
    {
        try
        {
            var session = await _backupService.RestoreBackupAsync(path).ConfigureAwait(false);
            if (session is not null)
            {
                RestoreFromSession(session);
                LastExportSummary = "Backup restored successfully.";
            }
        }
        catch (Exception ex)
        {
            LastExportSummary = $"Restore failed: {ex.Message}";
        }
    }

    public async Task DeleteBackupAsync(string path)
    {
        try
        {
            await _backupService.DeleteBackupAsync(path).ConfigureAwait(false);
            await RefreshBackupsAsync().ConfigureAwait(false);
        }
        catch
        {
            // Non-fatal.
        }
    }

    private async Task RefreshBackupsAsync()
    {
        try
        {
            var backupList = await _backupService.ListBackupsAsync().ConfigureAwait(false);
            Backups.Clear();
            foreach (var backup in backupList.Take(10))
            {
                Backups.Add(new BackupEntryViewModel(backup, _backupService));
            }
        }
        catch
        {
            // Non-fatal.
        }
    }
#pragma warning restore CA1031

    // ==================== AI Translation ====================

#pragma warning disable CA1031
    private async Task AiTranslateSelectedAsync()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        IsAiTranslating = true;
        AiStatusMessage = $"Translating with {SelectedAiProvider}...";

        try
        {
            var request = new AiTranslationRequest(
                "English",
                SelectedTargetCulture.DisplayName,
                [new AiTranslationRequestEntry(SelectedEntry.Key, SelectedEntry.SourceText)],
                _settings.AiTranslationContext);

            var result = await _aiManager.TranslateWithProviderAsync(
                SelectedAiProvider,
                request).ConfigureAwait(false);

            if (result.IsSuccess && result.Entries.Count > 0)
            {
                AiSuggestion = result.Entries[0].TranslatedText;
                AiSuggestionProvider = result.Provider.ToString();
                AiSuggestionModel = result.ModelUsed;
                AiSuggestionTokens = result.TokensUsed;
                AiSuggestionDurationMs = result.DurationMs;
                HasAiSuggestion = true;
                AiStatusMessage = $"✓ Suggestion ready ({result.DurationMs}ms, {result.TokensUsed} tokens)";
            }
            else
            {
                AiStatusMessage = $"✗ {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            AiStatusMessage = $"✗ Error: {ex.Message}";
        }
        finally
        {
            IsAiTranslating = false;
        }
    }

    private async Task AiTranslateAllMissingAsync()
    {
        IsAiTranslating = true;
        AiTranslationProgress = 0;
        AiStatusMessage = $"Batch translating with {SelectedAiProvider}...";

        try
        {
            var missingEntries = Entries
                .Where(e => e.State == TranslationReviewState.Missing || string.IsNullOrWhiteSpace(e.TargetText))
                .ToList();

            if (missingEntries.Count == 0)
            {
                AiStatusMessage = "No missing translations to process.";
                return;
            }

            // Batch in groups of 10
            int batchSize = 10;
            int processed = 0;
            int total = missingEntries.Count;

            for (int i = 0; i < total; i += batchSize)
            {
                var batch = missingEntries.Skip(i).Take(batchSize).ToList();
                var request = new AiTranslationRequest(
                    "English",
                    SelectedTargetCulture.DisplayName,
                    batch.Select(e => new AiTranslationRequestEntry(e.Key, e.SourceText)).ToList(),
                    _settings.AiTranslationContext);

                var result = await _aiManager.TranslateWithProviderAsync(
                    SelectedAiProvider,
                    request).ConfigureAwait(false);

                if (result.IsSuccess)
                {
                    foreach (var translated in result.Entries)
                    {
                        var entry = batch.FirstOrDefault(e => e.Key == translated.Key);
                        if (entry is not null && !string.IsNullOrWhiteSpace(translated.TranslatedText))
                        {
                            entry.TargetText = translated.TranslatedText;
                            entry.State = TranslationReviewState.Review;
                        }
                    }
                }
                else
                {
                    AiStatusMessage = $"✗ Batch error: {result.ErrorMessage}";
                    break;
                }

                processed += batch.Count;
                AiTranslationProgress = (double)processed / total * 100.0;
                AiStatusMessage = $"Translated {processed}/{total} entries...";
            }

            RaiseMetrics();
            RaiseSelectedEntryProperties();
            MarkDirty();

            if (AiTranslationProgress >= 100)
            {
                AiStatusMessage = $"✓ Batch complete: {total} entries translated.";
            }
        }
        catch (Exception ex)
        {
            AiStatusMessage = $"✗ Batch error: {ex.Message}";
        }
        finally
        {
            IsAiTranslating = false;
        }
    }
#pragma warning restore CA1031

    private void AcceptAiSuggestion()
    {
        if (SelectedEntry is null || !HasAiSuggestion)
        {
            return;
        }

        string oldValue = SelectedEntry.TargetText;
        CaptureUndo(SelectedEntry.Key, nameof(CatalogEntryViewModel.TargetText), oldValue, AiSuggestion);
        SelectedEntry.TargetText = AiSuggestion;
        SelectedEntry.State = TranslationReviewState.Review;
        RaiseSelectedEntryProperties();
        RaiseMetrics();
        MarkDirty();
        ClearAiSuggestion();
        AiStatusMessage = "✓ Suggestion accepted.";
    }

    private void RejectAiSuggestion()
    {
        ClearAiSuggestion();
        AiStatusMessage = "Suggestion rejected.";
    }

    private void ClearAiSuggestion()
    {
        AiSuggestion = string.Empty;
        AiSuggestionProvider = string.Empty;
        AiSuggestionModel = string.Empty;
        AiSuggestionTokens = 0;
        AiSuggestionDurationMs = 0;
        HasAiSuggestion = false;
    }

    // ==================== File-based Import/Export (called from code-behind) ====================

#pragma warning disable CA1031
    public async Task ImportFromFileAsync(string filePath)
    {
        try
        {
            var snapshot = await _catalogGateway.ImportFromFileAsync(filePath, SelectedImportFormat, SelectedTargetCulture).ConfigureAwait(false);
            LoadSnapshot(snapshot);
            MarkDirty();
            LastExportSummary = $"Imported from {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            LastExportSummary = $"Import failed: {ex.Message}";
        }
    }

    public async Task ExportToFileAsync(string filePath)
    {
        try
        {
            var snapshot = CreateSnapshotFromView();
            await _catalogGateway.ExportToFileAsync(snapshot, filePath, SelectedExportFormat).ConfigureAwait(false);
            LastExportSummary = $"Exported to {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            LastExportSummary = $"Export failed: {ex.Message}";
        }
    }
#pragma warning restore CA1031

    // ==================== Existing Methods (preserved) ====================

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _sessionService.StopAutoSave();
        _aiManager.Dispose();
        Strings.PropertyChanged -= OnStringsChanged;
        DetachEntryHandlers();
        Strings.Dispose();
        _disposed = true;
    }

    private void LoadDemoCatalog()
    {
        LoadSnapshot(_catalogGateway.LoadDemoCatalog(SelectedImportFormat, SelectedTargetCulture));
        LastExportSummary = Strings.ImportedStatus;
    }

#pragma warning disable CA1031
    private async void ImportFromClipboard()
    {
        try
        {
            var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                string text = await dataPackageView.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    try
                    {
                        var snapshot = _catalogGateway.ImportCatalog(text, SelectedImportFormat, SelectedTargetCulture);
                        LoadSnapshot(snapshot);
                        MarkDirty();
                        LastExportSummary = $"{Strings.ImportedStatus} (Imported from clipboard)";
                        return;
                    }
                    catch
                    {
                        // Fall through to default load on parse error
                    }
                }
            }
        }
        catch
        {
            // Fall through to default load on clipboard access failure
        }

        LoadDemoCatalog();
    }

    private void ExportCatalog()
    {
        TranslationCatalogSnapshot snapshot = CreateSnapshotFromView();
        string summary = _catalogGateway.CreateExportPreview(snapshot, SelectedExportFormat);
        string content = _catalogGateway.ExportCatalog(snapshot, SelectedExportFormat);

        try
        {
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(content);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            LastExportSummary = $"{summary} [Copied to clipboard!]";
        }
        catch (Exception ex)
        {
            LastExportSummary = $"{summary} [Clipboard copy failed: {ex.Message}]";
        }
    }
#pragma warning restore CA1031

    private void SetSelectedState(TranslationReviewState state)
    {
        if (SelectedEntry is null)
        {
            return;
        }

        string oldState = SelectedEntry.State.ToString();
        CaptureUndo(SelectedEntry.Key, nameof(CatalogEntryViewModel.State), oldState, state.ToString());
        SelectedEntry.State = state;
        RaiseSelectedEntryProperties();
        RaiseMetrics();
        MarkDirty();
    }

    private void LoadSnapshot(TranslationCatalogSnapshot snapshot)
    {
        _currentSnapshot = snapshot;
        DetachEntryHandlers();
        Entries.Clear();
        foreach (TranslationCatalogEntry entry in snapshot.Entries)
        {
            var viewModel = new CatalogEntryViewModel(entry);
            viewModel.PropertyChanged += OnEntryChanged;
            Entries.Add(viewModel);
        }

        Coverage.Clear();
        foreach (TranslationCoverageColumn coverage in snapshot.Coverage)
        {
            Coverage.Add(new CultureCoverageViewModel(coverage));
        }

        RefreshFilters();
        RaiseCatalogProperties();
        _exportCatalogCommand.RaiseCanExecuteChanged();
        _autoTranslateAllCommand.RaiseCanExecuteChanged();
        _aiTranslateAllMissingCommand.RaiseCanExecuteChanged();
    }

    private void RefreshFilters()
    {
        var previouslySelected = SelectedEntry;
        FilteredEntries.Clear();

        var query = Entries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(entry =>
                entry.Key.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                entry.SourceText.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                entry.TargetText.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedStateFilter != "All")
        {
            var state = SelectedStateFilter switch
            {
                "Approved" => TranslationReviewState.Approved,
                "Review" => TranslationReviewState.Review,
                _ => TranslationReviewState.Missing
            };
            query = query.Where(entry => entry.State == state);
        }

        foreach (var entry in query)
        {
            FilteredEntries.Add(entry);
        }

        if (previouslySelected is not null && FilteredEntries.Contains(previouslySelected))
        {
            SelectedEntry = previouslySelected;
        }
        else
        {
            SelectedEntry = FilteredEntries.FirstOrDefault();
        }
    }

    private void AddKey()
    {
        if (string.IsNullOrWhiteSpace(NewKeyName))
        {
            return;
        }

        var key = NewKeyName.Trim();
        var source = NewKeySource.Trim();

        if (Entries.Any(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var newEntry = new TranslationCatalogEntry(
            key,
            source,
            string.Empty,
            TranslationReviewState.Missing,
            "Added manually in Translation Studio.",
            "Missing target translation value.");

        var viewModel = new CatalogEntryViewModel(newEntry);
        viewModel.PropertyChanged += OnEntryChanged;
        Entries.Add(viewModel);

        NewKeyName = string.Empty;
        NewKeySource = string.Empty;

        RefreshFilters();
        SelectedEntry = viewModel;
        RaiseMetrics();
        MarkDirty();
    }

    private void DeleteSelectedKey()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        var entryToDelete = SelectedEntry;
        entryToDelete.PropertyChanged -= OnEntryChanged;
        Entries.Remove(entryToDelete);

        RefreshFilters();
        RaiseMetrics();
        MarkDirty();
    }

    private void AutoTranslateSelected()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        string oldValue = SelectedEntry.TargetText;
        string newValue = GetMockTranslation(SelectedEntry.Key, SelectedEntry.SourceText, SelectedTargetCulture.CultureName);
        CaptureUndo(SelectedEntry.Key, nameof(CatalogEntryViewModel.TargetText), oldValue, newValue);
        SelectedEntry.TargetText = newValue;
        SelectedEntry.State = TranslationReviewState.Approved;
        RaiseMetrics();
        RaiseSelectedEntryProperties();
        MarkDirty();
    }

    private void AutoTranslateAll()
    {
        foreach (var entry in Entries)
        {
            if (entry.State == TranslationReviewState.Missing || string.IsNullOrWhiteSpace(entry.TargetText))
            {
                entry.TargetText = GetMockTranslation(entry.Key, entry.SourceText, SelectedTargetCulture.CultureName);
                entry.State = TranslationReviewState.Approved;
            }
        }
        RaiseMetrics();
        RaiseSelectedEntryProperties();
        MarkDirty();
    }

    private void AddTargetCulture()
    {
        if (string.IsNullOrWhiteSpace(NewCultureCode) || string.IsNullOrWhiteSpace(NewCultureName))
        {
            return;
        }

        var code = NewCultureCode.Trim();
        var name = NewCultureName.Trim();

        if (TargetCultures.Any(c => string.Equals(c.CultureName, code, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var newChoice = new CultureChoice(code, name);
        TargetCultures.Add(newChoice);

        NewCultureCode = string.Empty;
        NewCultureName = string.Empty;

        SelectedTargetCulture = newChoice;
    }

    private static string GetMockTranslation(string key, string sourceText, string targetCulture)
    {
        var lowerCulture = targetCulture.ToLowerInvariant();
        if (lowerCulture.StartsWith("pl"))
        {
            return key switch
            {
                "Shell.FileMenu" => "Plik",
                "Shell.Import" => "Importuj katalog",
                "Shell.Export" => "Eksportuj katalog",
                "Orders.EmptyState" => "Brak zamowien do wyswietlenia.",
                "Orders.Total" => "Suma: {0}",
                "Orders.DueDate" => "Termin: {0:D}",
                "Region.MeasurementSystem" => "System miar: {0}",
                "Diagnostics.ProviderFailed" => "Dostawca {0} zwrocil blad: {1}",
                _ => $"[PL] {sourceText}"
            };
        }
        else if (lowerCulture.StartsWith("de"))
        {
            return key switch
            {
                "Shell.FileMenu" => "Datei",
                "Shell.Import" => "Katalog importieren",
                "Shell.Export" => "Katalog exportieren",
                "Orders.EmptyState" => "Keine Bestellungen zur Anzeige.",
                "Orders.Total" => "Summe: {0}",
                "Orders.DueDate" => "Faellig bis {0:D}",
                "Region.MeasurementSystem" => "Masssystem: {0}",
                "Diagnostics.ProviderFailed" => "Anbieter {0} ist fehlgeschlagen: {1}",
                _ => $"[DE] {sourceText}"
            };
        }
        else if (lowerCulture.StartsWith("ja"))
        {
            return key switch
            {
                "Shell.FileMenu" => "ファイル",
                "Shell.Import" => "カタログのインポート",
                "Shell.Export" => "カタログのエクスポート",
                "Orders.EmptyState" => "注文はありません。",
                "Orders.Total" => "合計: {0}",
                "Orders.DueDate" => "期限: {0:D}",
                "Region.MeasurementSystem" => "計測システム: {0}",
                "Diagnostics.ProviderFailed" => "プロバイダー {0} が失敗しました: {1}",
                _ => $"[JA] {sourceText}"
            };
        }
        return $"[{targetCulture.ToUpperInvariant()}] {sourceText}";
    }

    private void DetachEntryHandlers()
    {
        foreach (CatalogEntryViewModel entry in Entries)
        {
            entry.PropertyChanged -= OnEntryChanged;
        }
    }

    private TranslationCatalogSnapshot CreateSnapshotFromView()
    {
        return _currentSnapshot with
        {
            Entries = Entries.Select(entry => entry.ToEntry()).ToArray()
        };
    }

    private void OnEntryChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CatalogEntryViewModel.State) or nameof(CatalogEntryViewModel.TargetText) or nameof(CatalogEntryViewModel.Diagnostics))
        {
            RaiseMetrics();
            RaiseSelectedEntryProperties();
            MarkDirty();
        }
    }

    private void OnStringsChanged(object? sender, PropertyChangedEventArgs e)
    {
        RaiseLocalizedText();
    }

    private void RaiseCatalogProperties()
    {
        OnPropertyChanged(nameof(CatalogFileName));
        OnPropertyChanged(nameof(CulturePair));
        OnPropertyChanged(nameof(SourceFormat));
        RaiseMetrics();
    }

    private void RaiseMetrics()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ApprovedCount));
        OnPropertyChanged(nameof(ReviewCount));
        OnPropertyChanged(nameof(MissingCount));
        OnPropertyChanged(nameof(CoveragePercent));
        OnPropertyChanged(nameof(CoverageLabel));
    }

    private void RaiseSelectedEntryProperties()
    {
        OnPropertyChanged(nameof(SelectedKey));
        OnPropertyChanged(nameof(SelectedSourceText));
        OnPropertyChanged(nameof(SelectedTargetText));
        OnPropertyChanged(nameof(SelectedNotes));
        OnPropertyChanged(nameof(SelectedStateLabel));
        OnPropertyChanged(nameof(SelectedDiagnostics));
        OnPropertyChanged(nameof(SelectedEntryHasDiagnostics));
        OnPropertyChanged(nameof(SelectedEntryState));
    }

    private void RaiseLocalizedText()
    {
        OnPropertyChanged(nameof(Strings));
        RaiseSelectedEntryProperties();
        if (LastExportSummary is "Ready" or "Gotowe")
        {
            LastExportSummary = Strings.ReadyStatus;
        }
    }
}
