using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProTranslate.WinUI.Tests;

public sealed class WinUIAdapterLeakTests
{
    [ReleaseFact]
    public void DisposedBindingSourceIsReleasedByTranslationService()
    {
        Assembly? adapter = TryLoadWinUIAdapter();
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
        Assembly? adapter = TryLoadWinUIAdapter();
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
        Assembly? adapter = TryLoadWinUIAdapter();
        if (adapter is null)
        {
            return;
        }

        WeakReference weak = InstallTemporaryStaticBindingSource(adapter);

        ReplaceStaticBindingSource(adapter);

        LeakTestHelpers.AssertCollected(weak);
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
    private static WeakReference InstallTemporaryStaticBindingSource(Assembly adapter)
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = CreateTranslationService(cultures);
        IDisposable source = CreateBindingSource(adapter, service, cultures);
        var weak = new WeakReference(source);

        UseSource(adapter, source);

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReplaceStaticBindingSource(Assembly adapter)
    {
        UseSource(adapter, CreateFreshSource(adapter));
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
        Type sourceType = GetRequiredType(adapter, "ProTranslate.WinUI.TranslationBindingSource");
        return (IDisposable)Activator.CreateInstance(sourceType, service, cultures)!;
    }

    private static void UseSource(Assembly adapter, IDisposable source)
    {
        InvokeStatic(GetRequiredType(adapter, "ProTranslate.WinUI.TranslationService"), "UseSource", source);
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

    private static Assembly? TryLoadWinUIAdapter()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        string assemblyPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "ProTranslate.WinUI",
            "bin",
            "Release",
            "net10.0-windows10.0.19041.0",
            "ProTranslate.WinUI.dll");

        if (!File.Exists(assemblyPath))
        {
            if (string.Equals(Environment.GetEnvironmentVariable("PROTRANSLATE_REQUIRE_WINUI_LEAKS"), "1", StringComparison.Ordinal))
            {
                throw new FileNotFoundException(
                    "Build src/ProTranslate.WinUI in Release before running required WinUI leak tests.",
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
