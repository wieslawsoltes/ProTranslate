namespace ProTranslate;

/// <summary>
/// Provides data for ProTranslate diagnostic notifications.
/// </summary>
public sealed class ProTranslateDiagnosticEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProTranslateDiagnosticEventArgs"/> class.
    /// </summary>
    /// <param name="diagnostic">The diagnostic that was reported.</param>
    public ProTranslateDiagnosticEventArgs(ProTranslateDiagnostic diagnostic)
    {
        Diagnostic = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
    }

    /// <summary>
    /// Gets the diagnostic that was reported.
    /// </summary>
    public ProTranslateDiagnostic Diagnostic { get; }
}
