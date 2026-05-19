using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Default implementation of <see cref="ICultureService"/>.
/// </summary>
public sealed class CultureService : ICultureService
{
    private readonly CultureServiceOptions _options;
    private CultureInfo _currentCulture;
    private CultureInfo _currentUICulture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureService"/> class.
    /// </summary>
    public CultureService()
        : this(CultureInfo.CurrentUICulture)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureService"/> class.
    /// </summary>
    /// <param name="initialCulture">The initial culture.</param>
    public CultureService(CultureInfo initialCulture)
        : this(initialCulture, new CultureServiceOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureService"/> class.
    /// </summary>
    /// <param name="initialCulture">The initial culture.</param>
    /// <param name="options">The culture application options.</param>
    public CultureService(CultureInfo initialCulture, CultureServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(initialCulture);
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _currentCulture = initialCulture;
        _currentUICulture = initialCulture;
        ApplyCulture(initialCulture, initialCulture);
    }

    /// <inheritdoc />
    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <inheritdoc />
    public CultureInfo CurrentCulture => _currentCulture;

    /// <inheritdoc />
    public CultureInfo CurrentUICulture => _currentUICulture;

    /// <inheritdoc />
    public RegionInfo CurrentRegion => CreateRegion(CurrentCulture);

    /// <inheritdoc />
    public bool IsMetric => CurrentRegion.IsMetric;

    /// <inheritdoc />
    public TextFlowDirection FlowDirection =>
        CurrentUICulture.TextInfo.IsRightToLeft ? TextFlowDirection.RightToLeft : TextFlowDirection.LeftToRight;

    /// <inheritdoc />
    public void SetCulture(string cultureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);
        SetCulture(CultureInfo.GetCultureInfo(cultureName));
    }

    /// <inheritdoc />
    public void SetCulture(CultureInfo culture)
    {
        SetCulture(culture, culture);
    }

    /// <inheritdoc />
    public void SetCulture(CultureInfo culture, CultureInfo uiCulture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);

        if (string.Equals(_currentCulture.Name, culture.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(_currentUICulture.Name, uiCulture.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CultureInfo oldCulture = _currentCulture;
        CultureInfo oldUICulture = _currentUICulture;
        _currentCulture = culture;
        _currentUICulture = uiCulture;
        ApplyCulture(culture, uiCulture);
        CultureChanged?.Invoke(this, new CultureChangedEventArgs(oldCulture, oldUICulture, culture, uiCulture));
    }

    private void ApplyCulture(CultureInfo culture, CultureInfo uiCulture)
    {
        if (_options.ApplyToDefaultThread)
        {
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
        }

        if (_options.ApplyToCurrentThread)
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = uiCulture;
        }
    }

    private static RegionInfo CreateRegion(CultureInfo culture)
    {
        if (string.IsNullOrEmpty(culture.Name))
        {
            return RegionInfo.CurrentRegion;
        }

        CultureInfo specificCulture = culture.IsNeutralCulture
            ? CultureInfo.CreateSpecificCulture(culture.Name)
            : culture;

        return new RegionInfo(specificCulture.Name);
    }
}
