using Microsoft.Extensions.DependencyInjection;

namespace ProTranslate.Maui;

public static class ProTranslateMauiServiceCollectionExtensions
{
    public static IServiceCollection AddProTranslateMaui(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            sp.UseProTranslateMaui();
            return TranslationService.Source;
        });

        return services;
    }

    public static IServiceProvider UseProTranslateMaui(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        TranslationService.UseService(
            serviceProvider.GetRequiredService<global::ProTranslate.ITranslationService>(),
            serviceProvider.GetService<global::ProTranslate.ICultureService>());
        return serviceProvider;
    }
}
