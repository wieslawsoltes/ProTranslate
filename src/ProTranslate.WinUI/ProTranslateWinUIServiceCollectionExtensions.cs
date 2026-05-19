using Microsoft.Extensions.DependencyInjection;

namespace ProTranslate.WinUI;

public static class ProTranslateWinUIServiceCollectionExtensions
{
    public static IServiceCollection AddProTranslateWinUI(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            TranslationService.UseService(
                sp.GetRequiredService<global::ProTranslate.ITranslationService>(),
                sp.GetService<global::ProTranslate.ICultureService>());
            return TranslationService.Source;
        });

        return services;
    }
}
