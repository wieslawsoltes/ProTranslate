using Microsoft.UI.Xaml;

namespace ProTranslate.Uno.TranslationStudio;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the main application window for use with file pickers and windowing APIs.
    /// </summary>
    public static Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window
        {
            Content = new MainPage(),
        };
        MainWindow.Activate();
    }
}
