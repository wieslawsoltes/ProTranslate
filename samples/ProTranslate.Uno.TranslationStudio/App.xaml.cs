using Microsoft.UI.Xaml;

namespace ProTranslate.Uno.TranslationStudio;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new Window
        {
            Content = new MainPage(),
        };
        _window.Activate();
    }
}
