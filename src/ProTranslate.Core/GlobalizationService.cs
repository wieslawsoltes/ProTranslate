using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Default implementation of <see cref="IGlobalizationService"/>.
/// </summary>
public sealed class GlobalizationService : IGlobalizationService, IDisposable
{
    private bool _disposed;
    private readonly IRegionProfileProvider _regionProfileProvider;
    private readonly IMeasurementSystemResolver _measurementSystemResolver;
    private RegionInfo? _regionOverride;
    private MeasurementSystem? _measurementSystemOverride;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalizationService"/> class.
    /// </summary>
    /// <param name="cultures">The culture service.</param>
    /// <param name="translations">The translation service.</param>
    public GlobalizationService(ICultureService cultures, ITranslationService translations)
        : this(
            cultures,
            translations,
            new DefaultRegionProfileProvider(),
            new DefaultMeasurementSystemResolver(),
            new DefaultUnitConversionService(),
            new DefaultLocalizedUnitFormatter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalizationService"/> class.
    /// </summary>
    /// <param name="cultures">The culture service.</param>
    /// <param name="translations">The translation service.</param>
    /// <param name="regionProfileProvider">The region profile provider.</param>
    /// <param name="measurementSystemResolver">The measurement system resolver.</param>
    public GlobalizationService(
        ICultureService cultures,
        ITranslationService translations,
        IRegionProfileProvider regionProfileProvider,
        IMeasurementSystemResolver measurementSystemResolver)
        : this(
            cultures,
            translations,
            regionProfileProvider,
            measurementSystemResolver,
            new DefaultUnitConversionService(),
            new DefaultLocalizedUnitFormatter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalizationService"/> class.
    /// </summary>
    /// <param name="cultures">The culture service.</param>
    /// <param name="translations">The translation service.</param>
    /// <param name="regionProfileProvider">The region profile provider.</param>
    /// <param name="measurementSystemResolver">The measurement system resolver.</param>
    /// <param name="unitConverter">The unit conversion service.</param>
    /// <param name="unitFormatter">The localized unit formatter.</param>
    public GlobalizationService(
        ICultureService cultures,
        ITranslationService translations,
        IRegionProfileProvider regionProfileProvider,
        IMeasurementSystemResolver measurementSystemResolver,
        IUnitConversionService unitConverter,
        ILocalizedUnitFormatter unitFormatter)
    {
        Cultures = cultures ?? throw new ArgumentNullException(nameof(cultures));
        Translations = translations ?? throw new ArgumentNullException(nameof(translations));
        _regionProfileProvider = regionProfileProvider ?? throw new ArgumentNullException(nameof(regionProfileProvider));
        _measurementSystemResolver = measurementSystemResolver ?? throw new ArgumentNullException(nameof(measurementSystemResolver));
        UnitConverter = unitConverter ?? throw new ArgumentNullException(nameof(unitConverter));
        UnitFormatter = unitFormatter ?? throw new ArgumentNullException(nameof(unitFormatter));
        Cultures.CultureChanged += OnCultureChanged;
    }

    /// <inheritdoc />
    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <inheritdoc />
    public ICultureService Cultures { get; }

    /// <inheritdoc />
    public ITranslationService Translations { get; }

    /// <inheritdoc />
    public IUnitConversionService UnitConverter { get; }

    /// <inheritdoc />
    public ILocalizedUnitFormatter UnitFormatter { get; }

    /// <inheritdoc />
    public CultureInfo CurrentCulture => Cultures.CurrentCulture;

    /// <inheritdoc />
    public bool IsMetric => MeasurementSystem == MeasurementSystem.Metric;

    /// <inheritdoc />
    public MeasurementSystem MeasurementSystem => MeasurementSystemProfile.System;

    /// <inheritdoc />
    public RegionProfile RegionProfile => _regionProfileProvider.GetProfile(CurrentCulture, _regionOverride);

    /// <inheritdoc />
    public MeasurementSystemProfile MeasurementSystemProfile =>
        _measurementSystemResolver.Resolve(RegionProfile, _measurementSystemOverride);

    /// <inheritdoc />
    public TextFlowDirection FlowDirection => Cultures.FlowDirection;

    /// <inheritdoc />
    public void SetCulture(CultureInfo culture) => Cultures.SetCulture(culture);

    /// <inheritdoc />
    public LocalizedString GetString(string key) => Translations.GetString(key);

    /// <inheritdoc />
    public MeasurementValue ConvertMeasurement(double value, MeasurementUnit sourceUnit) =>
        UnitConverter.ConvertToProfile(value, sourceUnit, MeasurementSystemProfile);

    /// <inheritdoc />
    public string FormatMeasurement(double value, MeasurementUnit unit, string? format = null) =>
        UnitFormatter.Format(value, unit, CurrentCulture, format);

    /// <inheritdoc />
    public string FormatMeasurementForProfile(double value, MeasurementUnit sourceUnit, string? format = null) =>
        UnitFormatter.Format(ConvertMeasurement(value, sourceUnit), CurrentCulture, format);

    /// <inheritdoc />
    public void SetRegionOverride(RegionInfo region)
    {
        ArgumentNullException.ThrowIfNull(region);

        if (string.Equals(
            _regionOverride?.TwoLetterISORegionName,
            region.TwoLetterISORegionName,
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _regionOverride = region;
        NotifyGlobalizationChanged();
    }

    /// <inheritdoc />
    public void ClearRegionOverride()
    {
        if (_regionOverride is null)
        {
            return;
        }

        _regionOverride = null;
        NotifyGlobalizationChanged();
    }

    /// <inheritdoc />
    public void SetMeasurementSystemOverride(MeasurementSystem measurementSystem)
    {
        if (_measurementSystemOverride == measurementSystem)
        {
            return;
        }

        _measurementSystemOverride = measurementSystem;
        NotifyGlobalizationChanged();
    }

    /// <inheritdoc />
    public void ClearMeasurementSystemOverride()
    {
        if (_measurementSystemOverride is null)
        {
            return;
        }

        _measurementSystemOverride = null;
        NotifyGlobalizationChanged();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Cultures.CultureChanged -= OnCultureChanged;
        _disposed = true;
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e) => CultureChanged?.Invoke(this, e);

    private void NotifyGlobalizationChanged()
    {
        var culture = Cultures.CurrentCulture;
        var uiCulture = Cultures.CurrentUICulture;
        CultureChanged?.Invoke(this, new CultureChangedEventArgs(culture, uiCulture, culture, uiCulture));
    }
}
