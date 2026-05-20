using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ProTranslate;

/// <summary>
/// Binding-friendly localized string proxy that refreshes on culture changes.
/// </summary>
public sealed class ObservableLocalizedString : IObservableLocalizedString
{
    private readonly ITranslationService _translationService;
    private object?[] _arguments;
    private LocalizedString _localizedString;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableLocalizedString"/> class.
    /// </summary>
    /// <param name="translationService">The translation service.</param>
    /// <param name="key">The resource key.</param>
    /// <param name="arguments">The optional format arguments.</param>
    public ObservableLocalizedString(ITranslationService translationService, string key, params object?[] arguments)
    {
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
        _arguments = arguments ?? Array.Empty<object?>();
        _localizedString = _translationService.GetString(Key);
        _translationService.CultureChanged += OnCultureChanged;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public string Key { get; }

    /// <inheritdoc />
    public string Value => _arguments.Length == 0
        ? _localizedString.Value
        : _translationService.Format(Key, _arguments);

    /// <inheritdoc />
    public LocalizedString LocalizedString => _localizedString;

    /// <inheritdoc />
    public CultureInfo Culture => _localizedString.Culture;

    /// <inheritdoc />
    public void UpdateArguments(params object?[] arguments)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _arguments = arguments ?? Array.Empty<object?>();
        OnPropertyChanged(nameof(Value));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _translationService.CultureChanged -= OnCultureChanged;
        _disposed = true;
    }

    /// <summary>
    /// Returns the current localized value.
    /// </summary>
    /// <returns>The current localized value.</returns>
    public override string ToString() => Value;

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        _localizedString = _translationService.GetString(Key);
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(LocalizedString));
        OnPropertyChanged(nameof(Culture));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
