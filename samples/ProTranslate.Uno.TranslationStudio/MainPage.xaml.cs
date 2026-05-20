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
    }

    public TranslationStudioViewModel ViewModel { get; }

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
