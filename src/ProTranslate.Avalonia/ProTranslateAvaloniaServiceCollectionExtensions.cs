using Microsoft.Extensions.DependencyInjection;

namespace ProTranslate.Avalonia;

public static class ProTranslateAvaloniaServiceCollectionExtensions
{
    public static IServiceCollection AddProTranslateAvalonia(this IServiceCollection services)
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
