using Microsoft.Maui.Hosting;
using ProTranslate.Samples.Shared;

namespace ProTranslate.Maui.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SampleTranslationHost host = SampleTranslations.Create();
        SampleState.Host = host;
        ProTranslate.Maui.TranslationService.UseService(host.Translations, host.Cultures);

        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        return builder.Build();
    }
}
