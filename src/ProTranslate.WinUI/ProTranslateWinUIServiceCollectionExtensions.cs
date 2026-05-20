using Microsoft.Extensions.DependencyInjection;

namespace ProTranslate.WinUI;

public static class ProTranslateWinUIServiceCollectionExtensions
{
    public static IServiceCollection AddProTranslateWinUI(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            sp.UseProTranslateWinUI();
            return TranslationService.Source;
        });

        return services;
    }

    public static IServiceProvider UseProTranslateWinUI(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        TranslationService.UseService(
            serviceProvider.GetRequiredService<global::ProTranslate.ITranslationService>(),
            serviceProvider.GetService<global::ProTranslate.ICultureService>());
        return serviceProvider;
    }
}
