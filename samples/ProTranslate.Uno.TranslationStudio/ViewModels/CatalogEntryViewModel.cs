using System.Text.RegularExpressions;

namespace ProTranslate.Uno.TranslationStudio;

public sealed class CatalogEntryViewModel : ObservableObject
{
    private string _targetText;
    private TranslationReviewState _state;
    private string _notes;
    private string _diagnostics = string.Empty;
    private string _formatName = "ProTranslate JSON";
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

    public string FormatName
    {
        get => _formatName;
        set
        {
            if (SetProperty(ref _formatName, value))
            {
                RunDiagnosticCheck();
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

    public static List<string> GetNormalizedPlaceholders(string text, string formatName)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        string fmt = formatName?.ToLowerInvariant() ?? "";
        
        if (fmt.Contains("i18next"))
        {
            MatchCollection matches = Regex.Matches(text, @"\{\{([a-zA-Z0-9_]+)(?:,\s*[^}]+)?\}\}");
            return matches.Cast<Match>().Select(static m => "{{" + m.Groups[1].Value + "}}").Distinct().ToList();
        }
        else if (fmt.Contains("flutter") || fmt.Contains("arb") || fmt.Contains("stringsdict") || fmt.Contains("xcstrings"))
        {
            MatchCollection matches = Regex.Matches(text, @"\{([a-zA-Z_][a-zA-Z0-9_]*)");
            return matches.Cast<Match>().Select(static m => "{" + m.Groups[1].Value + "}").Distinct().ToList();
        }
        else if (fmt.Contains("resx") || fmt.Contains("protranslate") || fmt.Contains("csv") || fmt.Contains("tsv") || fmt.Contains("xliff"))
        {
            MatchCollection matches = Regex.Matches(text, @"\{([0-9]+)(?::[^}]+)?\}");
            return matches.Cast<Match>().Select(static m => "{" + m.Groups[1].Value + "}").Distinct().ToList();
        }
        else if (fmt.Contains("po") || fmt.Contains("pot") || fmt.Contains("android") || fmt.Contains("apple") || fmt.Contains("strings"))
        {
            MatchCollection matches = Regex.Matches(text, @"%(?:([0-9]+)\$)?([-+ #0]*[0-9]*(?:\.[0-9]+)?[lhjztL]*[diouxXeEfFgGaAcsp@%])");
            return matches.Cast<Match>().Select(static m => m.Groups[1].Success ? "%" + m.Groups[1].Value + "$" : m.Value).Distinct().ToList();
        }
        
        MatchCollection fallbackMatches = Regex.Matches(text, @"\{([^:},]+)");
        return fallbackMatches.Cast<Match>().Select(static m => "{" + m.Groups[1].Value.Trim() + "}").Distinct().ToList();
    }

    public static List<string> GetPlaceholdersForFormat(string text, string formatName)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        string fmt = formatName?.ToLowerInvariant() ?? "";
        
        if (fmt.Contains("i18next"))
        {
            MatchCollection matches = Regex.Matches(text, @"\{\{([^}]+)\}\}");
            return matches.Cast<Match>().Select(static m => m.Value).Distinct().ToList();
        }
        else if (fmt.Contains("flutter") || fmt.Contains("arb") || fmt.Contains("stringsdict") || fmt.Contains("xcstrings"))
        {
            MatchCollection matches = Regex.Matches(text, @"\{([a-zA-Z_][a-zA-Z0-9_]*(?:\s*,\s*[a-zA-Z]+(?:\s*,\s*[^}]+)?)?)\}");
            return matches.Cast<Match>().Select(static m => m.Value).Distinct().ToList();
        }
        else if (fmt.Contains("resx") || fmt.Contains("protranslate") || fmt.Contains("csv") || fmt.Contains("tsv") || fmt.Contains("xliff"))
        {
            MatchCollection matches = Regex.Matches(text, @"\{([0-9]+)(?::[^}]+)?\}");
            return matches.Cast<Match>().Select(static m => m.Value).Distinct().ToList();
        }
        else if (fmt.Contains("po") || fmt.Contains("pot") || fmt.Contains("android") || fmt.Contains("apple") || fmt.Contains("strings"))
        {
            MatchCollection matches = Regex.Matches(text, @"%(?:([0-9]+)\$)?([-+ #0]*[0-9]*(?:\.[0-9]+)?[lhjztL]*[diouxXeEfFgGaAcsp@%])");
            return matches.Cast<Match>().Select(static m => m.Value).Distinct().ToList();
        }
        
        MatchCollection fallbackMatches = Regex.Matches(text, @"\{([^}]+)\}");
        return fallbackMatches.Cast<Match>().Select(static m => m.Value).Distinct().ToList();
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
            List<string> sourcePlaceholders = GetNormalizedPlaceholders(SourceText, FormatName);
            List<string> targetPlaceholders = GetNormalizedPlaceholders(TargetText, FormatName);

            // Unbalanced braces check
            int openBraces = TargetText.Count(c => c == '{');
            int closeBraces = TargetText.Count(c => c == '}');
            if (openBraces != closeBraces)
            {
                errors.Add("Unbalanced brace brackets '{' and '}'");
            }

            foreach (string sp in sourcePlaceholders)
            {
                if (!targetPlaceholders.Contains(sp))
                {
                    errors.Add($"Placeholder '{sp}' is missing in target translation");
                }
            }

            foreach (string tp in targetPlaceholders)
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

