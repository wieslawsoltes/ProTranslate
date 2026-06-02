using System.Globalization;
using Aprillz.MewUI;

namespace ProTranslate.MewUI;

/// <summary>
/// Bridges ProTranslate's culture service with MewUI's ObservableValue binding system.
/// Holds weak references to all active ObservableValue&lt;string&gt; instances and updates
/// them automatically when the culture changes.
/// </summary>
public sealed class TranslationBindingSource : IDisposable
{
    private global::ProTranslate.ITranslationService _translationService;
    private global::ProTranslate.ICultureService? _cultureService;
    private readonly List<(string Key, WeakReference<ObservableValue<string>> Ref)> _bindings = [];
    private bool _disposed;

    public TranslationBindingSource(
        global::ProTranslate.ITranslationService translationService,
        global::ProTranslate.ICultureService? cultureService = null)
    {
        ArgumentNullException.ThrowIfNull(translationService);
        _translationService = translationService;
        _cultureService = cultureService;
        _translationService.CultureChanged += OnCultureChanged;
    }

    public global::ProTranslate.ITranslationService TranslationService => _translationService;

    public CultureInfo Culture
    {
        get => _translationService.CurrentUICulture;
        set => _cultureService?.SetCulture(value);
    }

    /// <summary>
    /// Creates an ObservableValue&lt;string&gt; bound to the given translation key.
    /// The value updates automatically whenever the culture changes.
    /// The returned ObservableValue can be passed directly to element.Bind(Property, obsValue).
    /// </summary>
    public ObservableValue<string> GetBinding(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var obs = new ObservableValue<string>(GetValue(key));
        _bindings.Add((key, new WeakReference<ObservableValue<string>>(obs)));
        return obs;
    }

    /// <summary>Gets the current translated value for a key without creating a binding.</summary>
    public string Translate(string key) => GetValue(key);

    /// <summary>Formats a translated string with arguments.</summary>
    public string Format(string key, params object?[] args) =>
        _translationService.Format(key, args);

    public void UseService(
        global::ProTranslate.ITranslationService translationService,
        global::ProTranslate.ICultureService? cultureService = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(translationService);

        if (!ReferenceEquals(_translationService, translationService))
        {
            _translationService.CultureChanged -= OnCultureChanged;
            _translationService = translationService;
            _translationService.CultureChanged += OnCultureChanged;
        }

        _cultureService = cultureService;
        RefreshAll();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _translationService.CultureChanged -= OnCultureChanged;
        _bindings.Clear();
        _disposed = true;
    }

    private string GetValue(string key)
    {
        var localized = _translationService.GetString(key);
        return localized.ResourceNotFound ? key : localized.Value;
    }

    private void OnCultureChanged(object? sender, global::ProTranslate.CultureChangedEventArgs e)
        => RefreshAll();

    private void RefreshAll()
    {
        var dead = new List<int>();
        for (int i = 0; i < _bindings.Count; i++)
        {
            if (_bindings[i].Ref.TryGetTarget(out var obs))
                obs.Value = GetValue(_bindings[i].Key);
            else
                dead.Add(i);
        }
        for (int i = dead.Count - 1; i >= 0; i--)
            _bindings.RemoveAt(dead[i]);
    }
}
