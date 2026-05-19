namespace ProTranslate;

/// <summary>
/// Receives structured diagnostics from ProTranslate services.
/// </summary>
public interface IProTranslateDiagnosticSink
{
    /// <summary>
    /// Reports a diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    void Report(ProTranslateDiagnostic diagnostic);
}
