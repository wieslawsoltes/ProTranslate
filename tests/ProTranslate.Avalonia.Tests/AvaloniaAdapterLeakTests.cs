using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Xunit;

namespace ProTranslate.Avalonia.Tests;

public sealed class AvaloniaAdapterLeakTests
{
    [ReleaseFact]
    public void DisposedBindingSourceIsReleasedByTranslationService()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);

        WeakReference weak = CreateDisposedBindingSource(service, cultures);

        AssertCollected(weak);
        GC.KeepAlive(service);
        GC.KeepAlive(cultures);
    }

    [ReleaseFact]
    public void DisposedObservableLocalizedStringIsReleasedByTranslationService()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);

        WeakReference weak = CreateDisposedObservableLocalizedString(service);

        AssertCollected(weak);
        GC.KeepAlive(service);
        GC.KeepAlive(cultures);
    }

    [ReleaseFact]
    public void AttachedTranslationTargetIsNotRootedBySubscription()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);
        ProTranslate.Avalonia.TranslationService.UseSource(new TranslationBindingSource(service, cultures));

        WeakReference weak = CreateAttachedTranslationTarget();

        AssertCollected(weak);
        GC.KeepAlive(service);
        GC.KeepAlive(cultures);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedBindingSource(
        global::ProTranslate.ITranslationService service,
        global::ProTranslate.ICultureService cultures)
    {
        var source = new TranslationBindingSource(service, cultures);
        var weak = new WeakReference(source);

        source.Dispose();

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedObservableLocalizedString(global::ProTranslate.ITranslationService service)
    {
        var observable = service.Observe("Shell.Title");
        var weak = new WeakReference(observable);

        observable.Dispose();

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAttachedTranslationTarget()
    {
        var textBlock = new TextBlock();
        var weak = new WeakReference(textBlock);

        Translation.SetKey(textBlock, "Shell.Title");

        return weak;
    }

    private static global::ProTranslate.TranslationService CreateTranslationService(CultureService cultures)
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Title", "Title");

        return new global::ProTranslate.TranslationService(provider, cultures);
    }

    private static void AssertCollected(WeakReference weak)
    {
        for (var i = 0; i < 10; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

            if (!weak.IsAlive)
            {
                return;
            }

            Thread.Sleep(10);
        }

        Assert.False(weak.IsAlive);
    }
}
