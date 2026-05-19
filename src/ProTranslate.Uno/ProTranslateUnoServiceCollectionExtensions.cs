using Microsoft.Extensions.DependencyInjection;

namespace ProTranslate.Uno;

public static class ProTranslateUnoServiceCollectionExtensions
{
    public static IServiceCollection AddProTranslateUno(this IServiceCollection services)
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
