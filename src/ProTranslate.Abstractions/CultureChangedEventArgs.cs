using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Provides data for culture change notifications.
/// </summary>
public sealed class CultureChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CultureChangedEventArgs"/> class.
    /// </summary>
    /// <param name="oldCulture">The previous culture.</param>
    /// <param name="newCulture">The current culture.</param>
    public CultureChangedEventArgs(CultureInfo oldCulture, CultureInfo newCulture)
        : this(oldCulture, oldCulture, newCulture, newCulture)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureChangedEventArgs"/> class.
    /// </summary>
    /// <param name="oldCulture">The previous formatting culture.</param>
    /// <param name="oldUICulture">The previous UI culture.</param>
    /// <param name="newCulture">The current formatting culture.</param>
    /// <param name="newUICulture">The current UI culture.</param>
    public CultureChangedEventArgs(
        CultureInfo oldCulture,
        CultureInfo oldUICulture,
        CultureInfo newCulture,
        CultureInfo newUICulture)
    {
        OldCulture = oldCulture;
        OldUICulture = oldUICulture;
        NewCulture = newCulture;
        NewUICulture = newUICulture;
    }

    /// <summary>
    /// Gets the previous culture.
    /// </summary>
    public CultureInfo OldCulture { get; }

    /// <summary>
    /// Gets the previous UI culture.
    /// </summary>
    public CultureInfo OldUICulture { get; }

    /// <summary>
    /// Gets the current formatting culture.
    /// </summary>
    public CultureInfo NewCulture { get; }

    /// <summary>
    /// Gets the current UI culture.
    /// </summary>
    public CultureInfo NewUICulture { get; }
}
