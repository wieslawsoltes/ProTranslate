using System.Windows;
using ProTranslate.Samples.Shared;

namespace ProTranslate.Wpf.Sample;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SampleTranslationHost host = SampleTranslations.Create();
        ProTranslate.Wpf.TranslationService.UseService(host.Translations, host.Cultures);

        MainWindow = new MainWindow(host);
        MainWindow.Show();
    }
}
