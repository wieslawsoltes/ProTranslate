using System.Globalization;

namespace ProTranslate.Wpf;

public static class TranslationService
{
    private static readonly global::ProTranslate.CultureService DefaultCultureService = new(CultureInfo.CurrentUICulture);
    private static readonly global::ProTranslate.ITranslationService DefaultTranslationService = new global::ProTranslate.TranslationService(
        new global::ProTranslate.InMemoryTranslationProvider(),
        DefaultCultureService);
    private static TranslationBindingSource _source = new(DefaultTranslationService, DefaultCultureService);

    internal static event EventHandler? SourceChanged;

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

        if (ReferenceEquals(_source, source))
        {
            _source.Refresh();
            return;
        }

        TranslationBindingSource previous = _source;
        _source = source;
        SourceChanged?.Invoke(null, EventArgs.Empty);
        previous.Dispose();
        _source.Refresh();
    }
}
