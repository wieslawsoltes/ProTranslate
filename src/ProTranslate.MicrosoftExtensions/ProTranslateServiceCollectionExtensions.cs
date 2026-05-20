using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace ProTranslate;

/// <summary>
/// Dependency injection helpers for ProTranslate core services.
/// </summary>
public static class ProTranslateServiceCollectionExtensions
{
    /// <summary>
    /// Registers ProTranslate core services with an optional provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="provider">The translation provider. If omitted, an empty in-memory provider is used.</param>
    /// <param name="culture">The initial culture. If omitted, <see cref="CultureInfo.CurrentUICulture"/> is used.</param>
    /// <param name="cultureOptions">The culture service options.</param>
    /// <param name="translationOptions">The translation fallback options.</param>
    /// <param name="cacheOptions">The translation cache options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddProTranslate(
        this IServiceCollection services,
        ITranslationProvider? provider = null,
        CultureInfo? culture = null,
        CultureServiceOptions? cultureOptions = null,
        TranslationFallbackOptions? translationOptions = null,
        TranslationCacheOptions? cacheOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(cultureOptions ?? new CultureServiceOptions());
        services.TryAddSingleton(translationOptions ?? new TranslationFallbackOptions());
        services.TryAddSingleton(cacheOptions ?? new TranslationCacheOptions());
        services.TryAddSingleton<ICultureService>(sp => new CultureService(
            culture ?? CultureInfo.CurrentUICulture,
            sp.GetRequiredService<CultureServiceOptions>()));
        services.TryAddSingleton(provider ?? new InMemoryTranslationProvider());
        services.TryAddSingleton<IRegionProfileProvider, DefaultRegionProfileProvider>();
        services.TryAddSingleton<IMeasurementSystemResolver, DefaultMeasurementSystemResolver>();
        services.TryAddSingleton<IUnitConversionService, DefaultUnitConversionService>();
        services.TryAddSingleton<ILocalizedUnitFormatter, DefaultLocalizedUnitFormatter>();
        services.TryAddSingleton<ITranslationService>(sp => new TranslationService(
            sp.GetRequiredService<ITranslationProvider>(),
            sp.GetRequiredService<ICultureService>(),
            sp.GetRequiredService<TranslationFallbackOptions>(),
            sp.GetRequiredService<TranslationCacheOptions>()));
        services.TryAddSingleton<ITranslationCacheInvalidator>(sp =>
            (ITranslationCacheInvalidator)sp.GetRequiredService<ITranslationService>());
        services.TryAddSingleton<IGlobalizationService>(sp => new GlobalizationService(
            sp.GetRequiredService<ICultureService>(),
            sp.GetRequiredService<ITranslationService>(),
            sp.GetRequiredService<IRegionProfileProvider>(),
            sp.GetRequiredService<IMeasurementSystemResolver>(),
            sp.GetRequiredService<IUnitConversionService>(),
            sp.GetRequiredService<ILocalizedUnitFormatter>()));

        return services;
    }

    /// <summary>
    /// Registers an <see cref="IStringLocalizer"/> based translation provider.
    /// </summary>
    /// <typeparam name="TResource">The resource marker type used by <see cref="IStringLocalizer{T}"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddProTranslateStringLocalizer<TResource>(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Replace(ServiceDescriptor.Singleton<ITranslationProvider>(sp =>
            new StringLocalizerTranslationProvider(sp.GetRequiredService<IStringLocalizer<TResource>>())));

        return services;
    }
}
