using System.Globalization;
using System.Runtime.CompilerServices;

namespace ProTranslate.Tests;

public sealed class CoreLeakTests
{
    [ReleaseFact]
    public void DisposedTranslationServiceIsReleasedByCultureService()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));

        WeakReference weak = CreateDisposedTranslationService(cultures);

        LeakTestHelpers.AssertCollected(weak);
        GC.KeepAlive(cultures);
    }

    [ReleaseFact]
    public void DisposedObservableLocalizedStringIsReleasedByTranslationService()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);

        WeakReference weak = CreateDisposedObservableLocalizedString(service, cultures);

        LeakTestHelpers.AssertCollected(weak);
        GC.KeepAlive(service);
        GC.KeepAlive(cultures);
    }

    [ReleaseFact]
    public void DisposedGlobalizationServiceIsReleasedByCultureService()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var translations = CreateTranslationService(cultures);

        WeakReference weak = CreateDisposedGlobalizationService(cultures, translations);

        LeakTestHelpers.AssertCollected(weak);
        GC.KeepAlive(cultures);
        GC.KeepAlive(translations);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedTranslationService(CultureService cultures)
    {
        var service = CreateTranslationService(cultures);
        var weak = new WeakReference(service);

        service.Dispose();

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedObservableLocalizedString(
        ITranslationService service,
        CultureService cultures)
    {
        IObservableLocalizedString observable = service.Observe("Shell.Title", 1);
        var weak = new WeakReference(observable);

        observable.UpdateArguments(2);
        cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));
        observable.Dispose();

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedGlobalizationService(
        ICultureService cultures,
        ITranslationService translations)
    {
        var globalization = new GlobalizationService(cultures, translations);
        var weak = new WeakReference(globalization);

        globalization.SetRegionOverride(new RegionInfo("US"));
        globalization.SetMeasurementSystemOverride(MeasurementSystem.Metric);
        globalization.Dispose();

        return weak;
    }

    private static TranslationService CreateTranslationService(CultureService cultures)
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Title", "Title {0}")
            .Add(CultureInfo.GetCultureInfo("pl-PL"), "Shell.Title", "Tytul {0}");

        return new TranslationService(provider, cultures);
    }
}

