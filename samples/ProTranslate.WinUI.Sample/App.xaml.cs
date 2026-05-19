using Microsoft.UI.Xaml;
using ProTranslate.Samples.Shared;

namespace ProTranslate.WinUI.Sample;

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
        ProTranslate.WinUI.TranslationService.UseService(host.Translations, host.Cultures);

        _window = new MainWindow(host);
        _window.Activate();
    }
}
