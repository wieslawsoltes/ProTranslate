using System.Globalization;
using ProTranslate.Generated;

namespace ProTranslate.Samples.Shared;

public sealed record SampleTranslationHost(
    global::ProTranslate.CultureService Cultures,
    global::ProTranslate.ITranslationService Translations);

public static class SampleTranslations
{
    public static SampleTranslationHost Create()
    {
        var defaultCulture = CultureInfo.GetCultureInfo("en-US");
        var cultures = new global::ProTranslate.CultureService(defaultCulture);
        var provider = new ProTranslateGeneratedTranslationProvider("SampleGeneratedCatalog");

        var options = new global::ProTranslate.TranslationFallbackOptions
        {
            DefaultCulture = defaultCulture
        };
        options.FallbackCultures.Add(defaultCulture);

        var translations = new global::ProTranslate.TranslationService(provider, cultures, options);
        return new SampleTranslationHost(cultures, translations);
    }
}
