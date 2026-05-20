using System.ComponentModel;
using System.Globalization;
using ProTranslate.Generated;

namespace ProTranslate.Samples.Shared;

public sealed class GeneratedTranslationProxy : ObservableObject, IDisposable
{
    private readonly ProTranslateStrings _strings;
    private readonly IUnitConversionService _unitConverter = new DefaultUnitConversionService();
    private readonly ILocalizedUnitFormatter _unitFormatter = new DefaultLocalizedUnitFormatter();
    private CultureInfo _culture;
    private RegionInfo _region;
    private bool _useImperialUnits = true;
    private bool _disposed;

    public GeneratedTranslationProxy(global::ProTranslate.ITranslationService translations)
    {
        ArgumentNullException.ThrowIfNull(translations);
        _strings = new ProTranslateStrings(translations);
        _strings.PropertyChanged += OnGeneratedStringsChanged;
        _culture = translations.CurrentCulture;
        _region = CreateRegion(_culture);
    }

    public ProTranslateStrings Generated => _strings;

    public CultureInfo Culture => _culture;

    public RegionInfo Region => _region;

    public bool UseImperialUnits => _useImperialUnits;

    public string AppTitle => _strings.AppTitle;

    public string FrameworkSubtitle => _strings.FrameworkSubtitle;

    public string LiveSwitchLabel => _strings.LiveSwitchLabel;

    public string RegionOverrideLabel => _strings.RegionOverrideLabel;

    public string FormattedStringsLabel => _strings.FormattedStringsLabel;

    public string UnitsLabel => _strings.UnitsLabel;

    public string MetricUnits => _strings.MetricUnits;

    public string ImperialUnits => _strings.ImperialUnits;

    public string OrderTitle => _strings.OrderTitle;

    public string GeneratedProxyLabel => _strings.GeneratedProxyLabel;

    public string MarkupExtensionLabel => _strings.MarkupExtensionLabel;

    public string SampleNote => _strings.SampleNote;

    public string FlowLabel => _strings.FlowLabel;

    public string CultureLabel => _strings.CultureLabel;

    public string RegionLabel => _strings.RegionLabel;

    public string UiFlowDirection => _culture.TextInfo.IsRightToLeft ? "RightToLeft" : "LeftToRight";

    public string UiTextAlignment => _culture.TextInfo.IsRightToLeft ? "Right" : "Left";

    public string FlowDirectionDisplay => _culture.TextInfo.IsRightToLeft ? _strings.DirectionRtl : _strings.DirectionLtr;

    public string CurrentCultureDisplay => _strings.Format_CurrentCultureFormat(_culture.NativeName, _culture.Name);

    public string CurrentRegionDisplay => _strings.Format_CurrentRegionFormat(_region.DisplayName, _region.TwoLetterISORegionName);

    public void SetCulture(CultureInfo culture, RegionInfo region, bool useImperialUnits)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(region);
        _culture = culture;
        _region = region;
        _useImperialUnits = useImperialUnits;
        RaiseAll();
    }

    public string Greeting(string customerName)
    {
        return _strings.Format_GreetingFormat(customerName);
    }

    public string InvoiceTotal(decimal total)
    {
        return _strings.Format_InvoiceTotalFormat(total.ToString("C", _culture));
    }

    public string FulfillmentSummary(int count, DateTimeOffset dueAt)
    {
        return _strings.Format_FulfillmentSummaryFormat(count.ToString("N0", _culture), dueAt.ToString("D", _culture));
    }

    public string Distance(double kilometers)
    {
        MeasurementUnit targetUnit = _useImperialUnits ? MeasurementUnit.Mile : MeasurementUnit.Kilometer;
        double value = _unitConverter.Convert(kilometers, MeasurementUnit.Kilometer, targetUnit);
        string measurement = _unitFormatter.Format(value, targetUnit, _culture, "N1");

        return _useImperialUnits
            ? _strings.Format_DistanceImperialFormat(measurement)
            : _strings.Format_DistanceMetricFormat(measurement);
    }

    public string Temperature(double celsius)
    {
        MeasurementUnit targetUnit = _useImperialUnits ? MeasurementUnit.Fahrenheit : MeasurementUnit.Celsius;
        double value = _unitConverter.Convert(celsius, MeasurementUnit.Celsius, targetUnit);
        string measurement = _unitFormatter.Format(value, targetUnit, _culture, "N0");

        return _useImperialUnits
            ? _strings.Format_TemperatureImperialFormat(measurement)
            : _strings.Format_TemperatureMetricFormat(measurement);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _strings.PropertyChanged -= OnGeneratedStringsChanged;
        _strings.Dispose();
        _disposed = true;
    }

    private void OnGeneratedStringsChanged(object? sender, PropertyChangedEventArgs e)
    {
        RaiseAll();
    }

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(Generated));
        OnPropertyChanged(nameof(Culture));
        OnPropertyChanged(nameof(Region));
        OnPropertyChanged(nameof(UseImperialUnits));
        OnPropertyChanged(nameof(AppTitle));
        OnPropertyChanged(nameof(FrameworkSubtitle));
        OnPropertyChanged(nameof(LiveSwitchLabel));
        OnPropertyChanged(nameof(RegionOverrideLabel));
        OnPropertyChanged(nameof(FormattedStringsLabel));
        OnPropertyChanged(nameof(UnitsLabel));
        OnPropertyChanged(nameof(MetricUnits));
        OnPropertyChanged(nameof(ImperialUnits));
        OnPropertyChanged(nameof(OrderTitle));
        OnPropertyChanged(nameof(GeneratedProxyLabel));
        OnPropertyChanged(nameof(MarkupExtensionLabel));
        OnPropertyChanged(nameof(SampleNote));
        OnPropertyChanged(nameof(FlowLabel));
        OnPropertyChanged(nameof(CultureLabel));
        OnPropertyChanged(nameof(RegionLabel));
        OnPropertyChanged(nameof(UiFlowDirection));
        OnPropertyChanged(nameof(UiTextAlignment));
        OnPropertyChanged(nameof(FlowDirectionDisplay));
        OnPropertyChanged(nameof(CurrentCultureDisplay));
        OnPropertyChanged(nameof(CurrentRegionDisplay));
    }

    private static RegionInfo CreateRegion(CultureInfo culture)
    {
        return string.IsNullOrEmpty(culture.Name)
            ? RegionInfo.CurrentRegion
            : new RegionInfo(culture.Name);
    }
}
