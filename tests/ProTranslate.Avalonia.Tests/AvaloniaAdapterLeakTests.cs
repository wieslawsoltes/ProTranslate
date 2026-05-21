using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Xunit;

namespace ProTranslate.Avalonia.Tests;

[Collection("AvaloniaHeadless")]
public sealed class AvaloniaAdapterLeakTests
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
    public void AttachedTranslationTargetIsNotRootedBySubscription()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);
        ProTranslate.Avalonia.TranslationService.UseSource(new TranslationBindingSource(service, cultures));

        (WeakReference Target, WeakReference Subscription) references = CreateAttachedTranslationTarget();

        LeakTestHelpers.AssertCollected(references.Target);
        ProTranslate.Avalonia.TranslationService.UseSource(CreateFreshSource());
        LeakTestHelpers.AssertCollected(references.Subscription);
        GC.KeepAlive(service);
        GC.KeepAlive(cultures);
    }

    [ReleaseFact]
    public void AttachedTargetsFromClosedWindowAreReleased()
    {
        WeakReference[] references = LeakTestSession.RunInSession(CreateAttachedTargetsInsideWindow);

        LeakTestHelpers.AssertCollected(references);
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

        ProTranslate.Avalonia.TranslationService.UseSource(source);
        ProTranslate.Avalonia.TranslationService.UseSource(CreateFreshSource());

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedObservableLocalizedString(
        global::ProTranslate.ITranslationService service,
        CultureService cultures)
    {
        IObservableLocalizedString observable = service.Observe("Shell.Title");
        var weak = new WeakReference(observable);

        cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));
        observable.Dispose();

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Target, WeakReference Subscription) CreateAttachedTranslationTarget()
    {
        var textBlock = new TextBlock();

        Translation.SetKey(textBlock, "Shell.Title");

        return (new WeakReference(textBlock), GetAttachedSubscriptionReference(textBlock));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CreateAttachedTargetsInsideWindow()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);
        ProTranslate.Avalonia.TranslationService.UseSource(new TranslationBindingSource(service, cultures));

        var textBlock = new TextBlock();
        var textBox = new TextBox();
        var contentControl = new ContentControl();
        var grid = new Grid();
        var panel = new StackPanel
        {
            Children =
            {
                textBlock,
                textBox,
                contentControl,
                grid
            }
        };
        var window = new Window
        {
            Width = 400,
            Height = 240,
            Content = panel
        };

        Translation.SetKey(textBlock, "Shell.Title");
        Translation.SetStringFormat(textBlock, "{0}!");
        Translation.SetKey(textBox, "Shell.Title");
        Translation.SetKey(contentControl, "Shell.Title");
        Translation.SetAutoFlowDirection(grid, true);

        LeakTestHelpers.ShowWindow(window);
        cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));
        LeakTestHelpers.RunJobsAndRender();

        WeakReference[] references =
        [
            new(window),
            new(panel),
            new(textBlock),
            new(textBox),
            new(contentControl),
            new(grid)
        ];

        LeakTestHelpers.CleanupWindow(window);
        ProTranslate.Avalonia.TranslationService.UseSource(CreateFreshSource());

        return references;
    }

    private static WeakReference GetAttachedSubscriptionReference(AvaloniaObject target)
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
