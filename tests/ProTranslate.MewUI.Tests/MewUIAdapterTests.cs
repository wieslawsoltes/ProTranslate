using System.Globalization;
using System.Runtime.CompilerServices;
using Aprillz.MewUI;
using ProTranslate.MewUI;

namespace ProTranslate.MewUI.Tests;

public sealed class MewUIAdapterTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    [Fact]
    public void GetBindingReturnsTranslatedValueForCurrentCulture()
    {
        var cultures = new CultureService(English);
        var source = new TranslationBindingSource(CreateService(cultures), cultures);

        ObservableValue<string> binding = source.GetBinding("Shell.Title");

        Assert.Equal("Title", binding.Value);
    }

    [Fact]
    public void BindingUpdatesWhenCultureChanges()
    {
        var cultures = new CultureService(English);
        var source = new TranslationBindingSource(CreateService(cultures), cultures);

        ObservableValue<string> binding = source.GetBinding("Shell.Title");
        Assert.Equal("Title", binding.Value);

        source.Culture = Polish;

        Assert.Equal("Tytul", binding.Value);
    }

    [Fact]
    public void MissingKeyFallsBackToKeyText()
    {
        var cultures = new CultureService(English);
        var source = new TranslationBindingSource(CreateService(cultures), cultures);

        ObservableValue<string> binding = source.GetBinding("Does.Not.Exist");

        Assert.Equal("Does.Not.Exist", binding.Value);
    }

    [Fact]
    public void StaticCulturePropertyReflectsActiveService()
    {
        var cultures = new CultureService(English);
        TranslationService.UseService(CreateService(cultures), cultures);

        Assert.Equal("Title", TranslationService.T("Shell.Title"));

        TranslationService.Culture = Polish;

        Assert.Equal("Tytul", TranslationService.T("Shell.Title"));
    }

    [ReleaseFact]
    public void DisposedBindingSourceIsReleasedByTranslationService()
    {
        var cultures = new CultureService(English);
        var service = CreateService(cultures);

        WeakReference weak = CreateDisposedBindingSource(service, cultures);

        LeakTestHelpers.AssertCollected(weak);
        GC.KeepAlive(service);
        GC.KeepAlive(cultures);
    }

    [ReleaseFact]
    public void BindingSourceServiceReplacementReleasesOldSubscriptions()
    {
        var firstCultures = new CultureService(English);
        var firstService = CreateService(firstCultures);
        var secondCultures = new CultureService(Polish);
        var secondService = CreateService(secondCultures);

        WeakReference weak = CreateSwappedAndDisposedBindingSource(
            firstService, firstCultures, secondService, secondCultures);

        LeakTestHelpers.AssertCollected(weak);
        GC.KeepAlive(firstService);
        GC.KeepAlive(firstCultures);
        GC.KeepAlive(secondService);
        GC.KeepAlive(secondCultures);
    }

    [ReleaseFact]
    public void UnreferencedBindingObservableIsNotRootedBySource()
    {
        var cultures = new CultureService(English);
        var source = new TranslationBindingSource(CreateService(cultures), cultures);

        WeakReference weak = CreateBinding(source);

        // The source only holds a WeakReference to issued bindings, so a dropped
        // binding must be collectable even while the source stays alive.
        LeakTestHelpers.AssertCollected(weak);

        // A culture change must still work (and prune the dead weak ref) without throwing.
        source.Culture = Polish;
        GC.KeepAlive(source);
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
    private static WeakReference CreateBinding(TranslationBindingSource source)
    {
        ObservableValue<string> binding = source.GetBinding("Shell.Title");
        return new WeakReference(binding);
    }

    private static global::ProTranslate.TranslationService CreateService(CultureService cultures)
    {
        var provider = new InMemoryTranslationProvider()
            .Add(English, "Shell.Title", "Title")
            .Add(Polish, "Shell.Title", "Tytul");

        return new global::ProTranslate.TranslationService(provider, cultures);
    }
}
