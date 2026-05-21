using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace ProTranslate.Uno.TranslationStudio;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        ViewModel = new TranslationStudioViewModel();
        InitializeComponent();
        Loaded += OnPageLoaded;
        KeyDown += OnPageKeyDown;
        ViewModel.InsertTextRequested += (s, text) => InsertTextAtCaret(text);
    }

    public TranslationStudioViewModel ViewModel { get; }

    private void InsertTextAtCaret(string text)
    {
        if (TargetTextBox == null) return;
        
        int selectionStart = TargetTextBox.SelectionStart;
        string currentText = TargetTextBox.Text ?? string.Empty;
        
        string newText = currentText.Substring(0, selectionStart) + text + currentText.Substring(selectionStart + TargetTextBox.SelectionLength);
        TargetTextBox.Text = newText;
        TargetTextBox.SelectionStart = selectionStart + text.Length;
        TargetTextBox.SelectionLength = 0;
        
        ViewModel.SelectedTargetText = newText;
        TargetTextBox.Focus(FocusState.Programmatic);
    }

    private void OnPlaceholderChipClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string placeholder)
        {
            InsertTextAtCaret(placeholder);
        }
    }

    private void OnTargetTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (TargetTextBox == null) return;
        
        int caretIndex = TargetTextBox.SelectionStart;
        string text = TargetTextBox.Text ?? string.Empty;
        
        if (caretIndex > 0)
        {
            int searchIndex = caretIndex - 1;
            string triggerText = string.Empty;
            
            if (searchIndex > 0 && text[searchIndex] == '{' && text[searchIndex - 1] == '{')
            {
                triggerText = "{{";
            }
            else if (text[searchIndex] == '{')
            {
                triggerText = "{";
            }
            else if (text[searchIndex] == '%')
            {
                triggerText = "%";
            }
            
            if (!string.IsNullOrEmpty(triggerText))
            {
                ViewModel.TriggerSuggestions(triggerText);
                return;
            }
        }
        
        ViewModel.ShowAutoComplete = false;
    }

    private void OnAutoCompleteListDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is string selection)
        {
            string cleanSelection = selection;
            int parenIndex = selection.IndexOf('(');
            if (parenIndex > 0)
            {
                cleanSelection = selection.Substring(0, parenIndex).Trim();
            }
            
            InsertTextAtCaret(cleanSelection);
            ViewModel.ShowAutoComplete = false;
        }
    }

    private void OnAutoCompleteListKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (sender is ListView listView && listView.SelectedItem is string selection)
            {
                string cleanSelection = selection;
                int parenIndex = selection.IndexOf('(');
                if (parenIndex > 0)
                {
                    cleanSelection = selection.Substring(0, parenIndex).Trim();
                }
                
                InsertTextAtCaret(cleanSelection);
                ViewModel.ShowAutoComplete = false;
                e.Handled = true;
            }
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            ViewModel.ShowAutoComplete = false;
            e.Handled = true;
            TargetTextBox.Focus(FocusState.Programmatic);
        }
    }

    public static Brush GetStateBrush(TranslationReviewState state)
    {
        string key = state switch
        {
            TranslationReviewState.Approved => "StudioGoodBrush",
            TranslationReviewState.Review => "StudioWarningBrush",
            _ => "StudioErrorBrush"
        };
        return (Brush)Application.Current.Resources[key];
    }

    public static Brush GetStateBgBrush(TranslationReviewState state)
    {
        string resKey = state switch
        {
            TranslationReviewState.Approved => "StudioGoodBgBrush",
            TranslationReviewState.Review => "StudioWarningBgBrush",
            _ => "StudioErrorBgBrush"
        };
        return (Brush)Application.Current.Resources[resKey];
    }

    public static Visibility VisibleIf(bool condition)
    {
        return condition ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility CollapsedIf(bool condition)
    {
        return condition ? Visibility.Collapsed : Visibility.Visible;
    }

    public static Visibility VisibleIfNotEmpty(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    public static Visibility VisibleIfType(string current, string expected)
    {
        return string.Equals(current, expected, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Brush GetSeverityBgBrush(string severity)
    {
        string key = severity?.ToLowerInvariant() switch
        {
            "success" => "StudioGoodBgBrush",
            "warning" => "StudioWarningBgBrush",
            _ => "StudioErrorBgBrush"
        };
        return (Brush)Application.Current.Resources[key];
    }

    public static Brush GetSeverityBorderBrush(string severity)
    {
        string key = severity?.ToLowerInvariant() switch
        {
            "success" => "StudioGoodBrush",
            "warning" => "StudioWarningBrush",
            _ => "StudioErrorBrush"
        };
        return (Brush)Application.Current.Resources[key];
    }

    public static string GetSeverityIcon(string severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "success" => "\xE930",
            "warning" => "\xE946",
            _ => "\xE7BA"
        };
    }

    public static Brush GetDirtyBrush(bool isDirty)
    {
        string key = isDirty ? "StudioDirtyDotBrush" : "StudioSavedDotBrush";
        return (Brush)Application.Current.Resources[key];
    }

    public static string FormatAiProvider(AiProvider provider)
    {
        return provider switch
        {
            AiProvider.OpenAI => "OpenAI",
            AiProvider.Claude => "Claude",
            AiProvider.Gemini => "Gemini",
            _ => provider.ToString()
        };
    }

#pragma warning disable CA1031
    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Check for crash recovery
        try
        {
            var recoverySession = await ViewModel.CheckRecoveryAsync();
            if (recoverySession is not null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Session Recovery",
                    Content = $"A recovered session was found from {recoverySession.LastModifiedAt.LocalDateTime:g} with {recoverySession.Entries.Count} entries.\n\nWould you like to restore it?",
                    PrimaryButtonText = "Restore",
                    SecondaryButtonText = "Discard",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    ViewModel.RestoreFromSession(recoverySession);
                }

                ViewModel.ClearRecovery();
            }
        }
        catch
        {
            // Recovery check failure is non-fatal.
        }
    }
#pragma warning restore CA1031

    private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
    {
        bool isCtrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (!isCtrl)
        {
            return;
        }

        switch (e.Key)
        {
            case Windows.System.VirtualKey.S:
                if (ViewModel.SaveSessionCommand.CanExecute(null))
                {
                    ViewModel.SaveSessionCommand.Execute(null);
                }
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Z:
                if (ViewModel.UndoCommand.CanExecute(null))
                {
                    ViewModel.UndoCommand.Execute(null);
                }
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Y:
                if (ViewModel.RedoCommand.CanExecute(null))
                {
                    ViewModel.RedoCommand.Execute(null);
                }
                e.Handled = true;
                break;
        }
    }

#pragma warning disable CA1031
    private async void OnImportFromFileClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file is not null)
            {
                await ViewModel.ImportFromFileAsync(file.Path);
            }
        }
        catch
        {
            // File picker failure is non-fatal — Uno may not support pickers on all platforms.
            // Fall back to clipboard import.
            ViewModel.ImportDemoCommand.Execute(null);
        }
    }

    private async void OnExportToFileClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = "translations";
            picker.FileTypeChoices.Add("Translation File", [ViewModel.SelectedExportFormat.Extension]);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file is not null)
            {
                await ViewModel.ExportToFileAsync(file.Path);
            }
        }
        catch
        {
            // File picker failure is non-fatal — fall back to clipboard export.
            ViewModel.ExportCatalogCommand.Execute(null);
        }
    }
#pragma warning restore CA1031

    private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SettingsVM is not null)
        {
            ViewModel.SettingsVM.IsVisible = true;
        }
    }

    private void OnCloseSettingsClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SettingsVM is not null)
        {
            ViewModel.SettingsVM.IsVisible = false;
        }
    }

#pragma warning disable CA1031
    private async void OnRestoreBackupClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string path)
        {
            try
            {
                await ViewModel.RestoreBackupAsync(path);
            }
            catch
            {
                // Restore failure handled by ViewModel.
            }
        }
    }

    private async void OnDeleteBackupClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string path)
        {
            try
            {
                await ViewModel.DeleteBackupAsync(path);
            }
            catch
            {
                // Delete failure handled by ViewModel.
            }
        }
    }
#pragma warning restore CA1031
}
