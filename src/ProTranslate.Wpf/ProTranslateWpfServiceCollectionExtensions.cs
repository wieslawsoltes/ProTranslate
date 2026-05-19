using Microsoft.Extensions.DependencyInjection;

namespace ProTranslate.Wpf;

public static class ProTranslateWpfServiceCollectionExtensions
{
    public static IServiceCollection AddProTranslateWpf(this IServiceCollection services)
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
