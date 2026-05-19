using Microsoft.Extensions.DependencyInjection;

namespace ProTranslate.Avalonia;

public static class ProTranslateAvaloniaServiceCollectionExtensions
{
    public static IServiceCollection AddProTranslateAvalonia(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            sp.UseProTranslateAvalonia();
            return TranslationService.Source;
        });

        return services;
    }

    public static IServiceProvider UseProTranslateAvalonia(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        TranslationService.UseService(
            serviceProvider.GetRequiredService<global::ProTranslate.ITranslationService>(),
            serviceProvider.GetService<global::ProTranslate.ICultureService>());
        return serviceProvider;
    }
}
