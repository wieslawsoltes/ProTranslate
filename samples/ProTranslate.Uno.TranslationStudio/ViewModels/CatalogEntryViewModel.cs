using System.Text.RegularExpressions;

namespace ProTranslate.Uno.TranslationStudio;

public sealed class CatalogEntryViewModel : ObservableObject
{
    private static readonly Regex PlaceholderRegex = new(@"\{([0-9]+)(:[^}]+)?\}", RegexOptions.Compiled);

    private string _targetText;
    private TranslationReviewState _state;
    private string _notes;
    private string _diagnostics = string.Empty;
    private readonly string _baseDiagnostics;

    public CatalogEntryViewModel(TranslationCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Key = entry.Key;
        SourceText = entry.SourceText;
        _targetText = entry.TargetText;
        _state = entry.State;
        _notes = entry.Notes;
        _baseDiagnostics = entry.Diagnostics;
        
        RunDiagnosticCheck();
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
                OnPropertyChanged(nameof(StateColor));
            }
        }
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string Diagnostics
    {
        get => _diagnostics;
        private set
        {
            if (SetProperty(ref _diagnostics, value))
            {
                OnPropertyChanged(nameof(HasDiagnostics));
                OnPropertyChanged(nameof(StateTone));
                OnPropertyChanged(nameof(StateColor));
            }
        }
    }

    public string StateLabel => State switch
    {
        TranslationReviewState.Approved => "Approved",
        TranslationReviewState.Review => "Review",
        _ => "Missing"
    };

    public string StateTone => State switch
    {
        TranslationReviewState.Approved => HasDiagnostics ? "Check" : "Ready",
        TranslationReviewState.Review => "Check",
        _ => "Blocker"
    };

    public string StateColor => State switch
    {
        TranslationReviewState.Approved => HasDiagnostics ? "#F59E0B" : "#10B981",
        TranslationReviewState.Review => "#F59E0B",
        _ => "#EF4444"
    };

    public bool HasDiagnostics => !string.IsNullOrWhiteSpace(Diagnostics) && Diagnostics != "No diagnostics";

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

        RunDiagnosticCheck();
    }

    private void RunDiagnosticCheck()
    {
        var errors = new List<string>();
        if (!string.IsNullOrWhiteSpace(_baseDiagnostics) && _baseDiagnostics != "No diagnostics")
        {
            errors.Add(_baseDiagnostics);
        }

        if (string.IsNullOrWhiteSpace(TargetText))
        {
            errors.Add("Missing target translation value.");
        }
        else
        {
            var sourceMatches = PlaceholderRegex.Matches(SourceText);
            var targetMatches = PlaceholderRegex.Matches(TargetText);

            var sourcePlaceholders = sourceMatches.Select(m => m.Value).Distinct().ToList();
            var targetPlaceholders = targetMatches.Select(m => m.Value).Distinct().ToList();

            foreach (var sp in sourcePlaceholders)
            {
                if (!targetPlaceholders.Contains(sp))
                {
                    errors.Add($"Placeholder '{sp}' is missing in target translation");
                }
            }

            foreach (var tp in targetPlaceholders)
            {
                if (!sourcePlaceholders.Contains(tp))
                {
                    errors.Add($"Mismatched placeholder '{tp}' found in target translation");
                }
            }
        }

        Diagnostics = errors.Count > 0 ? string.Join("; ", errors) : "No diagnostics";
    }
}
