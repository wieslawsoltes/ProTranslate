using Microsoft.Extensions.DependencyInjection;

namespace ProTranslate.Wpf;

public static class ProTranslateWpfServiceCollectionExtensions
{
    public static IServiceCollection AddProTranslateWpf(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            sp.UseProTranslateWpf();
            return TranslationService.Source;
        });

        return services;
    }

    public static IServiceProvider UseProTranslateWpf(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        TranslationService.UseService(
            serviceProvider.GetRequiredService<global::ProTranslate.ITranslationService>(),
            serviceProvider.GetService<global::ProTranslate.ICultureService>());
        return serviceProvider;
    }
}
