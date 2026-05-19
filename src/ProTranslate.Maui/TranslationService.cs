using System.Globalization;

namespace ProTranslate.Maui;

public static class TranslationService
{
    private static readonly global::ProTranslate.CultureService DefaultCultureService = new(CultureInfo.CurrentUICulture);
    private static readonly global::ProTranslate.ITranslationService DefaultTranslationService = new global::ProTranslate.TranslationService(
        new global::ProTranslate.InMemoryTranslationProvider(),
        DefaultCultureService);
    private static TranslationBindingSource _source = new(DefaultTranslationService, DefaultCultureService);

    public static TranslationBindingSource Source => _source;

    public static CultureInfo Culture
    {
        get => _source.Culture;
        set => _source.Culture = value;
    }

    public static string T(string key)
    {
        return _source.Translate(key);
    }

    public static string Translate(string key)
    {
        return _source.Translate(key);
    }

    public static void UseService(
        global::ProTranslate.ITranslationService translationService,
        global::ProTranslate.ICultureService? cultureService = null)
    {
        _source.UseService(translationService, cultureService);
    }

    public static void UseSource(TranslationBindingSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _source.Refresh();
    }
}
