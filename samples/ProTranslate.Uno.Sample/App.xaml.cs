using Microsoft.UI.Xaml;
using ProTranslate.Samples.Shared;

namespace ProTranslate.Uno.Sample;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        SampleTranslationHost host = SampleTranslations.Create();
        ProTranslate.Uno.TranslationService.UseService(host.Translations, host.Cultures);

        _window = new Window
        {
            Content = new MainPage(host),
        };
        _window.Activate();
    }
}
