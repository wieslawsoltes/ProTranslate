using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace ProTranslate.Maui;

public sealed class TranslationBindingSource : INotifyPropertyChanged, IDisposable
{
    private global::ProTranslate.ITranslationService _translationService;
    private global::ProTranslate.ICultureService? _cultureService;
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public global::ProTranslate.ITranslationService TranslationService => _translationService;

    public CultureInfo Culture
    {
        get => _translationService.CurrentUICulture;
        set => _cultureService?.SetCulture(value);
    }

    public object? this[string key] => ToBindingValue(_translationService.GetString(key));

    public string Translate(string key, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return arguments.Length == 0
            ? _translationService.GetString(key).Value
            : _translationService.Format(key, arguments);
    }

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
        NotifyCultureChanged();
    }

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _translationService.CultureChanged -= OnCultureChanged;
        _disposed = true;
    }

    private void OnCultureChanged(object? sender, global::ProTranslate.CultureChangedEventArgs e)
    {
        NotifyCultureChanged();
    }

    private void NotifyCultureChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
        Refresh();
    }

    private static object? ToBindingValue(global::ProTranslate.LocalizedString localized) =>
        localized.ResourceNotFound ? BindableProperty.UnsetValue : localized.Value;
}
