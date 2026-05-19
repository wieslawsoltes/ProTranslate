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
    private readonly TranslationService _translationService;
    private readonly RelayCommand _approveSelectedCommand;
    private readonly RelayCommand _markReviewCommand;
    private readonly RelayCommand _exportCatalogCommand;
    private CultureChoice _selectedUiCulture;
    private CultureChoice _selectedTargetCulture;
    private CatalogFormatChoice _selectedImportFormat;
    private CatalogFormatChoice _selectedExportFormat;
    private CatalogEntryViewModel? _selectedEntry;
    private TranslationCatalogSnapshot _currentSnapshot;
    private string _lastExportSummary;
    private bool _disposed;

    public TranslationStudioViewModel()
        : this(new InMemoryTranslationCatalogGateway())
    {
    }

    public TranslationStudioViewModel(ITranslationCatalogGateway catalogGateway)
    {
        _catalogGateway = catalogGateway ?? throw new ArgumentNullException(nameof(catalogGateway));

        UiCultures =
        [
            new CultureChoice("en-US", "English"),
            new CultureChoice("pl-PL", "Polski")
        ];
        TargetCultures =
        [
            new CultureChoice("pl-PL", "Polish"),
            new CultureChoice("de-DE", "German"),
            new CultureChoice("ja-JP", "Japanese")
        ];
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
        _translationService = new TranslationService(provider, _cultureService, options);
        Strings = new ProTranslateStrings(_translationService);
        Strings.PropertyChanged += OnStringsChanged;

        ImportDemoCommand = new RelayCommand(_ => ImportDemoCatalog());
        _exportCatalogCommand = new RelayCommand(_ => ExportCatalog(), _ => Entries.Count > 0);
        _approveSelectedCommand = new RelayCommand(_ => SetSelectedState(TranslationReviewState.Approved), _ => SelectedEntry is not null);
        _markReviewCommand = new RelayCommand(_ => SetSelectedState(TranslationReviewState.Review), _ => SelectedEntry is not null);

        ExportCatalogCommand = _exportCatalogCommand;
        ApproveSelectedCommand = _approveSelectedCommand;
        MarkReviewCommand = _markReviewCommand;

        _currentSnapshot = _catalogGateway.LoadDemoCatalog(_selectedImportFormat, _selectedTargetCulture);
        _lastExportSummary = Strings.ReadyStatus;
        LoadSnapshot(_currentSnapshot);
    }

    public ProTranslateStrings Strings { get; }

    public IReadOnlyList<CultureChoice> UiCultures { get; }

    public IReadOnlyList<CultureChoice> TargetCultures { get; }

    public IReadOnlyList<CatalogFormatChoice> ImportFormats { get; }

    public IReadOnlyList<CatalogFormatChoice> ExportFormats { get; }

    public ObservableCollection<CatalogEntryViewModel> Entries { get; } = [];

    public ObservableCollection<CultureCoverageViewModel> Coverage { get; } = [];

    public ICommand ImportDemoCommand { get; }

    public ICommand ExportCatalogCommand { get; }

    public ICommand ApproveSelectedCommand { get; }

    public ICommand MarkReviewCommand { get; }

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
                ImportDemoCatalog();
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
                ImportDemoCatalog();
            }
        }
    }

    public CatalogFormatChoice SelectedExportFormat
    {
        get => _selectedExportFormat;
        set => SetProperty(ref _selectedExportFormat, value);
    }

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

            SelectedEntry.TargetText = value;
            RaiseMetrics();
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
            }
        }
    }

    public string SelectedStateLabel => SelectedEntry?.StateLabel ?? Strings.NoSelection;

    public string SelectedDiagnostics => SelectedEntry?.Diagnostics ?? Strings.NoDiagnostics;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Strings.PropertyChanged -= OnStringsChanged;
        DetachEntryHandlers();
        Strings.Dispose();
        _disposed = true;
    }

    private void ImportDemoCatalog()
    {
        LoadSnapshot(_catalogGateway.LoadDemoCatalog(SelectedImportFormat, SelectedTargetCulture));
        LastExportSummary = Strings.ImportedStatus;
    }

    private void ExportCatalog()
    {
        TranslationCatalogSnapshot snapshot = CreateSnapshotFromView();
        LastExportSummary = _catalogGateway.CreateExportPreview(snapshot, SelectedExportFormat);
    }

    private void SetSelectedState(TranslationReviewState state)
    {
        if (SelectedEntry is null)
        {
            return;
        }

        SelectedEntry.State = state;
        RaiseSelectedEntryProperties();
        RaiseMetrics();
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

        SelectedEntry = Entries.FirstOrDefault();
        RaiseCatalogProperties();
        _exportCatalogCommand.RaiseCanExecuteChanged();
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
        if (e.PropertyName is nameof(CatalogEntryViewModel.State) or nameof(CatalogEntryViewModel.TargetText))
        {
            RaiseMetrics();
            RaiseSelectedEntryProperties();
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
