namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Captures a single undoable/redoable change for the undo/redo stack.
/// </summary>
public sealed record UndoRedoEntry(
    string Key,
    string PropertyName,
    string OldValue,
    string NewValue,
    DateTimeOffset Timestamp);
