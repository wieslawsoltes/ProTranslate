using System.Globalization;

namespace ProTranslate.MewUI;

/// <summary>
/// Static entry point for the ProTranslate MewUI adapter.
/// Call UseService() once at app startup, then use the fluent extension methods.
/// </summary>
public static class TranslationService
{
    private static readonly global::ProTranslate.CultureService DefaultCultureService =
        new(CultureInfo.CurrentUICulture);

    private static readonly global::ProTranslate.ITranslationService DefaultTranslationService =
        new global::ProTranslate.TranslationService(
            new global::ProTranslate.InMemoryTranslationProvider(),
            DefaultCultureService);

    private static TranslationBindingSource _source =
        new(DefaultTranslationService, DefaultCultureService);

    internal static event EventHandler? SourceChanged;

    public static TranslationBindingSource Source => _source;

    public static CultureInfo Culture
    {
        get => _source.Culture;
        set => _source.Culture = value;
    }

    /// <summary>Gets the current translated string for a key.</summary>
    public static string T(string key) => _source.Translate(key);

    /// <summary>Gets the current translated string for a key (alias for T).</summary>
    public static string Translate(string key) => _source.Translate(key);

    /// <summary>
    /// Registers the ProTranslate services to use for all MewUI translation bindings.
    /// Call this once during app initialization before building any windows.
    /// </summary>
    public static void UseService(
        global::ProTranslate.ITranslationService translationService,
        global::ProTranslate.ICultureService? cultureService = null)
    {
        _source.UseService(translationService, cultureService);
    }

    /// <summary>Replaces the active binding source entirely.</summary>
    public static void UseSource(TranslationBindingSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (ReferenceEquals(_source, source))
        {
            return;
        }

        TranslationBindingSource previous = _source;
        _source = source;
        SourceChanged?.Invoke(null, EventArgs.Empty);
        previous.Dispose();
    }
}
