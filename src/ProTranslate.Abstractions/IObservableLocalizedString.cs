using System.ComponentModel;
using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Represents a binding-friendly localized string that updates when culture changes.
/// </summary>
public interface IObservableLocalizedString : INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// Gets the resource key.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Gets the current localized value.
    /// </summary>
    string Value { get; }

    /// <summary>
    /// Gets the current localized lookup result.
    /// </summary>
    LocalizedString LocalizedString { get; }

    /// <summary>
    /// Gets the culture used for the current value.
    /// </summary>
    CultureInfo Culture { get; }

    /// <summary>
    /// Replaces the format arguments and refreshes the value.
    /// </summary>
    /// <param name="arguments">The new format arguments.</param>
    void UpdateArguments(params object?[] arguments);
}
