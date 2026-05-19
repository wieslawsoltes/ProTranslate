namespace ProTranslate.Uno.TranslationStudio;

public sealed class CultureCoverageViewModel
{
    public CultureCoverageViewModel(TranslationCoverageColumn coverage)
    {
        ArgumentNullException.ThrowIfNull(coverage);

        CultureName = coverage.CultureName;
        DisplayName = coverage.DisplayName;
        Completeness = coverage.Completeness;
    }

    public string CultureName { get; }

    public string DisplayName { get; }

    public double Completeness { get; }

    public double CompletenessPercent => Completeness * 100d;

    public string CompletenessLabel => Completeness.ToString("P0", System.Globalization.CultureInfo.CurrentCulture);
}
