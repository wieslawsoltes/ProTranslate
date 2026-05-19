using Microsoft.Extensions.DependencyInjection;

namespace ProTranslate.Maui;

public static class ProTranslateMauiServiceCollectionExtensions
{
    public static IServiceCollection AddProTranslateMaui(this IServiceCollection services)
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
