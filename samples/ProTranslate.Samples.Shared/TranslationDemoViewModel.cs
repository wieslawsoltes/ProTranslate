using System.Globalization;
using System.Windows.Input;

namespace ProTranslate.Samples.Shared;

public sealed class TranslationDemoViewModel : ObservableObject, IDisposable
{
    private static readonly DateTimeOffset DueDate = new(2026, 6, 18, 9, 30, 0, TimeSpan.Zero);
    private readonly decimal _invoiceTotal = 24890.75m;
    private readonly double _distanceKilometers = 42.2;
    private readonly double _temperatureCelsius = 21;
    private readonly SampleTranslationHost _host;
    private CultureChoice _selectedCultureChoice;
    private RegionChoice _selectedRegionChoice;
    private bool _useImperialUnits;
    private string _customerName = "Marta";

    public TranslationDemoViewModel()
        : this(SampleTranslations.Create())
    {
    }

    public TranslationDemoViewModel(SampleTranslationHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        Strings = new GeneratedTranslationProxy(_host.Translations);
        CultureChoices =
        [
            new CultureChoice("en-US", "US", "English / United States"),
            new CultureChoice("pl-PL", "PL", "Polski / Polska"),
            new CultureChoice("ar-SA", "SA", "العربية / السعودية")
        ];
        RegionChoices =
        [
            new RegionChoice(null, "Auto from culture"),
            new RegionChoice("US", "United States"),
            new RegionChoice("PL", "Poland"),
            new RegionChoice("SA", "Saudi Arabia"),
            new RegionChoice("JP", "Japan")
        ];

        _selectedCultureChoice = CultureChoices[0];
        _selectedRegionChoice = RegionChoices[0];
        _useImperialUnits = true;

        SetMetricCommand = new RelayCommand(_ => UseImperialUnits = false);
        SetImperialCommand = new RelayCommand(_ => UseImperialUnits = true);
        ApplyCulture();
    }

    public GeneratedTranslationProxy Strings { get; }

    public IReadOnlyList<CultureChoice> CultureChoices { get; }

    public IReadOnlyList<RegionChoice> RegionChoices { get; }

    public ICommand SetMetricCommand { get; }

    public ICommand SetImperialCommand { get; }

    public CultureChoice SelectedCultureChoice
    {
        get => _selectedCultureChoice;
        set
        {
            if (SetProperty(ref _selectedCultureChoice, value))
            {
                ApplyCulture();
            }
        }
    }

    public RegionChoice SelectedRegionChoice
    {
        get => _selectedRegionChoice;
        set
        {
            if (SetProperty(ref _selectedRegionChoice, value))
            {
                var region = ResolveRegion();
                UseImperialUnits = UsesImperialUnits(region);
                ApplyCulture();
            }
        }
    }

    public bool UseImperialUnits
    {
        get => _useImperialUnits;
        set
        {
            if (SetProperty(ref _useImperialUnits, value))
            {
                ApplyCulture();
            }
        }
    }

    public string CustomerName
    {
        get => _customerName;
        set
        {
            if (SetProperty(ref _customerName, value))
            {
                RaiseComputedTranslations();
            }
        }
    }

    public string GreetingText => Strings.Greeting(CustomerName);

    public string InvoiceTotalText => Strings.InvoiceTotal(_invoiceTotal);

    public string FulfillmentText => Strings.FulfillmentSummary(3, DueDate);

    public string DistanceText => Strings.Distance(_distanceKilometers);

    public string TemperatureText => Strings.Temperature(_temperatureCelsius);

    public string UnitsDisplay => UseImperialUnits ? Strings.ImperialUnits : Strings.MetricUnits;

    public string FlowDirectionName => Strings.UiFlowDirection;

    public string TextAlignmentName => Strings.UiTextAlignment;

    public void Dispose()
    {
        Strings.Dispose();
    }

    private void ApplyCulture()
    {
        var culture = CultureInfo.GetCultureInfo(SelectedCultureChoice.CultureName);
        var region = ResolveRegion();
        _host.Cultures.SetCulture(culture);
        Strings.SetCulture(culture, region, UseImperialUnits);
        RaiseComputedTranslations();
    }

    private RegionInfo ResolveRegion()
    {
        return new RegionInfo(SelectedRegionChoice.RegionName ?? SelectedCultureChoice.DefaultRegionName);
    }

    private static bool UsesImperialUnits(RegionInfo region)
    {
        return region.TwoLetterISORegionName is "US" or "LR" or "MM";
    }

    private void RaiseComputedTranslations()
    {
        OnPropertyChanged(nameof(Strings));
        OnPropertyChanged(nameof(GreetingText));
        OnPropertyChanged(nameof(InvoiceTotalText));
        OnPropertyChanged(nameof(FulfillmentText));
        OnPropertyChanged(nameof(DistanceText));
        OnPropertyChanged(nameof(TemperatureText));
        OnPropertyChanged(nameof(UnitsDisplay));
        OnPropertyChanged(nameof(FlowDirectionName));
        OnPropertyChanged(nameof(TextAlignmentName));
    }
}
