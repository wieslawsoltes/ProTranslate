using System.Globalization;
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace ProTranslate.MewUI;

/// <summary>
/// Fluent extension methods for binding ProTranslate translation keys to MewUI elements.
/// All methods use the global TranslationService.Source and return the element
/// for chaining, consistent with MewUI's C# markup style.
/// </summary>
public static class TranslationExtensions
{
    // ── TextBlock ────────────────────────────────────────────────────────────

    /// <summary>Binds TextBlock.Text to a translation key. Updates automatically on culture change.</summary>
    public static TextBlock Translate(this TextBlock tb, string key)
        => tb.Bind(TextBlock.TextProperty, TranslationService.Source.GetBinding(key));

    /// <summary>Binds TextBlock.Text using a custom source (for multi-source scenarios).</summary>
    public static TextBlock Translate(this TextBlock tb, string key, TranslationBindingSource source)
        => tb.Bind(TextBlock.TextProperty, source.GetBinding(key));

    // ── Window ───────────────────────────────────────────────────────────────

    /// <summary>Binds Window.Title to a translation key.</summary>
    public static Window TranslateTitle(this Window w, string key)
        => w.Bind(Window.TitleProperty, TranslationService.Source.GetBinding(key));

    /// <summary>Binds Window.Title using a custom source.</summary>
    public static Window TranslateTitle(this Window w, string key, TranslationBindingSource source)
        => w.Bind(Window.TitleProperty, source.GetBinding(key));

    // ── Culture switcher ─────────────────────────────────────────────────────

    /// <summary>
    /// Switches the global translation culture and returns the element for chaining.
    /// Typically called from a ComboBox.OnSelectionChanged or Button.OnClick.
    /// </summary>
    public static T UseCulture<T>(this T element, string cultureName) where T : Element
    {
        TranslationService.Culture = CultureInfo.GetCultureInfo(cultureName);
        return element;
    }

    // ── Inline string helper (no binding, current value only) ─────────────────

    /// <summary>
    /// Returns the current translated value of a key without creating a reactive binding.
    /// Use for one-time setup or non-reactive contexts.
    /// </summary>
    public static string T(this object _, string key) => TranslationService.T(key);

    // ── ObservableValue<string> factory ───────────────────────────────────────

    /// <summary>
    /// Creates a reactive ObservableValue&lt;string&gt; for a key that can be passed to any
    /// element's .Bind(SomeStringProperty, obs) call.
    /// </summary>
    public static ObservableValue<string> TranslationBinding(string key)
        => TranslationService.Source.GetBinding(key);

    /// <summary>
    /// Creates a reactive ObservableValue&lt;string&gt; from a custom source.
    /// </summary>
    public static ObservableValue<string> TranslationBinding(string key, TranslationBindingSource source)
        => source.GetBinding(key);
}
