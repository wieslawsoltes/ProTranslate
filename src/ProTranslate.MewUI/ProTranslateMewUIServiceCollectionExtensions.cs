using Microsoft.Extensions.DependencyInjection;

namespace ProTranslate.MewUI;

public static class ProTranslateMewUIServiceCollectionExtensions
{
    public static IServiceCollection AddProTranslateMewUI(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            sp.UseProTranslateMewUI();
            return TranslationService.Source;
        });

        return services;
    }

    public static IServiceProvider UseProTranslateMewUI(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        TranslationService.UseService(
            serviceProvider.GetRequiredService<global::ProTranslate.ITranslationService>(),
            serviceProvider.GetService<global::ProTranslate.ICultureService>());
        return serviceProvider;
    }
}
