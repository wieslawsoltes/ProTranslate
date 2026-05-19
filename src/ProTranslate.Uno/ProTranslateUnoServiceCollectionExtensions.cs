using Microsoft.Extensions.DependencyInjection;

namespace ProTranslate.Uno;

public static class ProTranslateUnoServiceCollectionExtensions
{
    public static IServiceCollection AddProTranslateUno(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            sp.UseProTranslateUno();
            return TranslationService.Source;
        });

        return services;
    }

    public static IServiceProvider UseProTranslateUno(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        TranslationService.UseService(
            serviceProvider.GetRequiredService<global::ProTranslate.ITranslationService>(),
            serviceProvider.GetService<global::ProTranslate.ICultureService>());
        return serviceProvider;
    }
}
