using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ProTranslate.Samples.Shared;

namespace ProTranslate.Avalonia.Sample;

public sealed partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SampleTranslationHost host = SampleTranslations.Create();
        ProTranslate.Avalonia.TranslationService.UseService(host.Translations, host.Cultures);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(host);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
