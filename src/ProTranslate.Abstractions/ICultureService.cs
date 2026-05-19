using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Manages the application culture and exposes globalization metadata.
/// </summary>
public interface ICultureService
{
    /// <summary>
    /// Occurs when the active culture changes.
    /// </summary>
    event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <summary>
    /// Gets the active culture.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets the active UI culture used for translation lookup.
    /// </summary>
    CultureInfo CurrentUICulture { get; }

    /// <summary>
    /// Gets the active region inferred from the active culture.
    /// </summary>
    RegionInfo CurrentRegion { get; }

    /// <summary>
    /// Gets a value indicating whether the active region uses the metric system.
    /// </summary>
    bool IsMetric { get; }

    /// <summary>
    /// Gets the natural text flow direction for the active culture.
    /// </summary>
    TextFlowDirection FlowDirection { get; }

    /// <summary>
    /// Sets the active culture.
    /// </summary>
    /// <param name="culture">The culture to activate.</param>
    void SetCulture(CultureInfo culture);

    /// <summary>
    /// Sets the active culture and UI culture.
    /// </summary>
    /// <param name="culture">The culture used for formatting.</param>
    /// <param name="uiCulture">The culture used for translation lookup.</param>
    void SetCulture(CultureInfo culture, CultureInfo uiCulture);

    /// <summary>
    /// Sets the active culture by name.
    /// </summary>
    /// <param name="cultureName">The culture name to activate.</param>
    void SetCulture(string cultureName);
}
