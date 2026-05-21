using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace ProTranslate.Maui.Tests;

public sealed class MauiAdapterLeakTests
{
    [ReleaseFact]
    public void DisposedBindingSourceIsReleasedByTranslationService()
    {
        Assembly? adapter = TryLoadMauiAdapter();
        if (adapter is null)
        {
            return;
        }

        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);

        WeakReference weak = CreateDisposedBindingSource(adapter, service, cultures);

        LeakTestHelpers.AssertCollected(weak);
        GC.KeepAlive(service);
        GC.KeepAlive(cultures);
    }

    [ReleaseFact]
    public void BindingSourceServiceReplacementReleasesOldAndCurrentSubscriptions()
    {
        Assembly? adapter = TryLoadMauiAdapter();
        if (adapter is null)
        {
            return;
        }

        var firstCultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var firstService = CreateTranslationService(firstCultures);
        var secondCultures = new CultureService(CultureInfo.GetCultureInfo("pl-PL"));
        var secondService = CreateTranslationService(secondCultures);

        WeakReference weak = CreateSwappedAndDisposedBindingSource(
            adapter,
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
        Assembly? adapter = TryLoadMauiAdapter();
        if (adapter is null)
        {
            return;
        }

        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);

        WeakReference weak = CreateReplacedStaticBindingSource(adapter, service, cultures);

        LeakTestHelpers.AssertCollected(weak);
        GC.KeepAlive(service);
        GC.KeepAlive(cultures);
    }

    [ReleaseFact]
    public void AttachedLabelTargetIsNotRootedBySubscription()
    {
        Assembly? adapter = TryLoadMauiAdapter();
        if (adapter is null)
        {
            return;
        }

        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);
        UseSource(adapter, CreateBindingSource(adapter, service, cultures));

        (WeakReference Target, WeakReference Subscription) references = CreateAttachedLabelTarget(adapter);

        LeakTestHelpers.AssertCollected(references.Target);
        UseSource(adapter, CreateFreshSource(adapter));
        LeakTestHelpers.AssertCollected(references.Subscription);
        GC.KeepAlive(service);
        GC.KeepAlive(cultures);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedBindingSource(
        Assembly adapter,
        global::ProTranslate.ITranslationService service,
        global::ProTranslate.ICultureService cultures)
    {
        IDisposable source = CreateBindingSource(adapter, service, cultures);
        var weak = new WeakReference(source);

        source.Dispose();

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateSwappedAndDisposedBindingSource(
        Assembly adapter,
        global::ProTranslate.ITranslationService firstService,
        global::ProTranslate.ICultureService firstCultures,
        global::ProTranslate.ITranslationService secondService,
        global::ProTranslate.ICultureService secondCultures)
    {
        IDisposable source = CreateBindingSource(adapter, firstService, firstCultures);
        var weak = new WeakReference(source);

        InvokeInstance(source, "UseService", secondService, secondCultures);
        source.Dispose();

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateReplacedStaticBindingSource(
        Assembly adapter,
        global::ProTranslate.ITranslationService service,
        global::ProTranslate.ICultureService cultures)
    {
        IDisposable source = CreateBindingSource(adapter, service, cultures);
        var weak = new WeakReference(source);

        UseSource(adapter, source);
        UseSource(adapter, CreateFreshSource(adapter));

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Target, WeakReference Subscription) CreateAttachedLabelTarget(Assembly adapter)
    {
        var label = new Label();

        InvokeStatic(GetRequiredType(adapter, "ProTranslate.Maui.Translation"), "SetKey", label, "Shell.Title");

        return (new WeakReference(label), GetAttachedSubscriptionReference(adapter, label));
    }

    private static IDisposable CreateFreshSource(Assembly adapter)
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        return CreateBindingSource(adapter, CreateTranslationService(cultures), cultures);
    }

    private static IDisposable CreateBindingSource(
        Assembly adapter,
        global::ProTranslate.ITranslationService service,
        global::ProTranslate.ICultureService cultures)
    {
        Type sourceType = GetRequiredType(adapter, "ProTranslate.Maui.TranslationBindingSource");
        return (IDisposable)Activator.CreateInstance(sourceType, service, cultures)!;
    }

    private static void UseSource(Assembly adapter, IDisposable source)
    {
        InvokeStatic(GetRequiredType(adapter, "ProTranslate.Maui.TranslationService"), "UseSource", source);
    }

    private static WeakReference GetAttachedSubscriptionReference(Assembly adapter, BindableObject target)
    {
        FieldInfo tableField = GetRequiredType(adapter, "ProTranslate.Maui.Translation").GetField(
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

    private static void InvokeInstance(object instance, string methodName, params object?[] arguments)
    {
        MethodInfo method = instance.GetType().GetMethods()
            .Single(method => string.Equals(method.Name, methodName, StringComparison.Ordinal)
                && method.GetParameters().Length == arguments.Length);
        method.Invoke(instance, arguments);
    }

    private static void InvokeStatic(Type type, string methodName, params object?[] arguments)
    {
        MethodInfo method = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Single(method => string.Equals(method.Name, methodName, StringComparison.Ordinal)
                && method.GetParameters().Length == arguments.Length);
        method.Invoke(null, arguments);
    }

    private static Type GetRequiredType(Assembly adapter, string typeName) =>
        adapter.GetType(typeName, throwOnError: true)!;

    private static Assembly? TryLoadMauiAdapter()
    {
        string assemblyPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "ProTranslate.Maui",
            "bin",
            "Release",
            "net10.0-maccatalyst",
            "ProTranslate.Maui.dll");

        if (!File.Exists(assemblyPath))
        {
            if (string.Equals(Environment.GetEnvironmentVariable("PROTRANSLATE_REQUIRE_MAUI_LEAKS"), "1", StringComparison.Ordinal))
            {
                throw new FileNotFoundException(
                    "Build src/ProTranslate.Maui in Release before running required MAUI leak tests.",
                    assemblyPath);
            }

            return null;
        }

        return Assembly.LoadFrom(assemblyPath);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }

    private static global::ProTranslate.TranslationService CreateTranslationService(CultureService cultures)
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Title", "Title")
            .Add(CultureInfo.GetCultureInfo("pl-PL"), "Shell.Title", "Tytul");

        return new global::ProTranslate.TranslationService(provider, cultures);
    }
}

