namespace ProTranslate.Uno.TranslationStudio;

public sealed class CatalogEntryViewModel : ObservableObject
{
    private string _targetText;
    private TranslationReviewState _state;
    private string _notes;

    public CatalogEntryViewModel(TranslationCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Key = entry.Key;
        SourceText = entry.SourceText;
        _targetText = entry.TargetText;
        _state = entry.State;
        _notes = entry.Notes;
        Diagnostics = entry.Diagnostics;
    }

    public string Key { get; }

    public string SourceText { get; }

    public string TargetText
    {
        get => _targetText;
        set
        {
            if (SetProperty(ref _targetText, value))
            {
                UpdateStateFromTarget();
            }
        }
    }

    public TranslationReviewState State
    {
        get => _state;
        set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(StateLabel));
                OnPropertyChanged(nameof(StateTone));
            }
        }
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string Diagnostics { get; }

    public string StateLabel => State switch
    {
        TranslationReviewState.Approved => "Approved",
        TranslationReviewState.Review => "Review",
        _ => "Missing"
    };

    public string StateTone => State switch
    {
        TranslationReviewState.Approved => "Ready",
        TranslationReviewState.Review => "Check",
        _ => "Blocker"
    };

    public bool HasDiagnostics => !string.IsNullOrWhiteSpace(Diagnostics);

    public TranslationCatalogEntry ToEntry()
    {
        return new TranslationCatalogEntry(Key, SourceText, TargetText, State, Notes, Diagnostics);
    }

    private void UpdateStateFromTarget()
    {
        if (string.IsNullOrWhiteSpace(TargetText))
        {
            State = TranslationReviewState.Missing;
        }
        else if (State == TranslationReviewState.Missing)
        {
            State = TranslationReviewState.Review;
        }
    }
}
