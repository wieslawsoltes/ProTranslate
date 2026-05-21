using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ProTranslate.Wpf;

namespace ProTranslate.Wpf.Tests;

public sealed class WpfAdapterLeakTests
{
    [ReleaseFact]
    public void DisposedBindingSourceIsReleasedByTranslationService()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);

        WeakReference weak = CreateDisposedBindingSource(service, cultures);

        LeakTestHelpers.AssertCollected(weak);
        GC.KeepAlive(service);
        GC.KeepAlive(cultures);
    }

    [ReleaseFact]
    public void BindingSourceServiceReplacementReleasesOldAndCurrentSubscriptions()
    {
        var firstCultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var firstService = CreateTranslationService(firstCultures);
        var secondCultures = new CultureService(CultureInfo.GetCultureInfo("pl-PL"));
        var secondService = CreateTranslationService(secondCultures);

        WeakReference weak = CreateSwappedAndDisposedBindingSource(
            firstService,
            firstCultures,
            secondService,
            secondCultures);

        LeakTestHelpers.AssertCollected(weak);
        GC.KeepAlive(firstService);
        GC.KeepAlive(firstCultures);
        GC.KeepAlive(secondService);
        GC.KeepAlive(secondCultures);
    }

    [ReleaseFact]
    public void ReplacedStaticBindingSourceIsReleased()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);

        WeakReference weak = CreateReplacedStaticBindingSource(service, cultures);

        LeakTestHelpers.AssertCollected(weak);
        GC.KeepAlive(service);
        GC.KeepAlive(cultures);
    }

    [ReleaseFact]
    public void AttachedTranslationTargetIsNotRootedBySubscription()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);
        TranslationService.UseSource(new TranslationBindingSource(service, cultures));

        (WeakReference Target, WeakReference Subscription) references = RunOnStaThread(CreateAttachedTranslationTarget);

        LeakTestHelpers.AssertCollected(references.Target);
        TranslationService.UseSource(CreateFreshSource());
        LeakTestHelpers.AssertCollected(references.Subscription);
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
    private static WeakReference CreateSwappedAndDisposedBindingSource(
        global::ProTranslate.ITranslationService firstService,
        global::ProTranslate.ICultureService firstCultures,
        global::ProTranslate.ITranslationService secondService,
        global::ProTranslate.ICultureService secondCultures)
    {
        var source = new TranslationBindingSource(firstService, firstCultures);
        var weak = new WeakReference(source);

        source.UseService(secondService, secondCultures);
        source.Dispose();

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateReplacedStaticBindingSource(
        global::ProTranslate.ITranslationService service,
        global::ProTranslate.ICultureService cultures)
    {
        var source = new TranslationBindingSource(service, cultures);
        var weak = new WeakReference(source);

        TranslationService.UseSource(source);
        TranslationService.UseSource(CreateFreshSource());

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Target, WeakReference Subscription) CreateAttachedTranslationTarget()
    {
        var textBlock = new TextBlock();

        Translation.SetKey(textBlock, "Shell.Title");

        return (new WeakReference(textBlock), GetAttachedSubscriptionReference(textBlock));
    }

    private static WeakReference GetAttachedSubscriptionReference(DependencyObject target)
    {
        FieldInfo tableField = typeof(Translation).GetField(
            "AttachedTargets",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Attached target table was not found.");
        object table = tableField.GetValue(null)
            ?? throw new InvalidOperationException("Attached target table was not initialized.");
        MethodInfo tryGetValue = table.GetType().GetMethod("TryGetValue")
            ?? throw new InvalidOperationException("Attached target table TryGetValue method was not found.");
        object?[] arguments = [target, null];
        var found = (bool)tryGetValue.Invoke(table, arguments)!;

        Assert.True(found);
        return new WeakReference(arguments[1]!);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The helper captures and rethrows arbitrary assertion or dispatcher failures from the STA thread.")]
    private static T RunOnStaThread<T>(Func<T> action)
    {
        T? result = default;
        ExceptionDispatchInfo? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                exception = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        exception?.Throw();

        return result!;
    }

    private static TranslationBindingSource CreateFreshSource()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        return new TranslationBindingSource(CreateTranslationService(cultures), cultures);
    }

    private static global::ProTranslate.TranslationService CreateTranslationService(CultureService cultures)
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Title", "Title")
            .Add(CultureInfo.GetCultureInfo("pl-PL"), "Shell.Title", "Tytul");

        return new global::ProTranslate.TranslationService(provider, cultures);
    }
}
