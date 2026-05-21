using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace ProTranslate.Avalonia.Tests;

internal static class LeakTestHelpers
{
    internal static void AssertCollected(params WeakReference[] references)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            CollectGarbage();

            if (references.All(static reference => !reference.IsAlive))
            {
                return;
            }

            Thread.Sleep(10);
        }

        foreach (WeakReference reference in references)
        {
            Assert.False(reference.IsAlive);
        }
    }

    internal static void ShowWindow(Window window)
    {
        if (window.SizeToContent != SizeToContent.Manual)
        {
            window.SizeToContent = SizeToContent.Manual;
        }

        if (double.IsNaN(window.Width) || window.Width <= 0)
        {
            window.Width = 800;
        }

        if (double.IsNaN(window.Height) || window.Height <= 0)
        {
            window.Height = 600;
        }

        window.Show();
        for (var i = 0; i < 3; i++)
        {
            ExecuteLayoutPass(window);
            RunJobsAndRender();
        }

        window.UpdateLayout();
        RunJobsAndRender();
    }

    internal static void CleanupWindow(Window window)
    {
        ClearFocusedElement();
        window.Content = null;
        window.DataContext = null;

        for (var i = 0; i < 3; i++)
        {
            ExecuteLayoutPass(window);
            RunJobsAndRender();
        }

        window.Close();
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        RunJobsAndRender();
    }

    internal static void CollectGarbage()
    {
        ClearFocusedElement();
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        RunJobsAndRender();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        RunJobsAndRender();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    internal static void ResetHeadlessCompositor()
    {
        Type? type = Type.GetType("Avalonia.Headless.AvaloniaHeadlessPlatform, Avalonia.Headless");
        PropertyInfo? property = type?.GetProperty("Compositor", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        property?.SetValue(null, null);
    }

    internal static void ResetLoadedQueueForUnitTests()
    {
        MethodInfo? resetMethod = typeof(Control).GetMethod(
            "ResetLoadedQueueForUnitTests",
            BindingFlags.Static | BindingFlags.NonPublic);
        resetMethod?.Invoke(null, null);
    }

    internal static void StopDispatcherTimers()
    {
        try
        {
            MethodInfo? snapshotMethod = typeof(Dispatcher).GetMethod(
                "SnapshotTimersForUnitTests",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (snapshotMethod?.Invoke(null, null) is IEnumerable timers)
            {
                foreach (object timer in timers)
                {
                    if (timer is DispatcherTimer dispatcherTimer)
                    {
                        dispatcherTimer.Stop();
                    }
                }
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or TargetInvocationException or ObjectDisposedException)
        {
            // Dispatcher state may already be gone after a headless session is disposed.
        }
    }

    internal static void RunJobsAndRender()
    {
        Dispatcher dispatcher = Dispatcher.UIThread;
        for (var i = 0; i < 10; i++)
        {
            dispatcher.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            if (!dispatcher.HasJobsWithPriority(DispatcherPriority.SystemIdle))
            {
                return;
            }
        }

        dispatcher.RunJobs();
    }

    private static void ClearFocusedElement()
    {
        object? keyboard = typeof(KeyboardDevice)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)
            ?.GetValue(null);
        MethodInfo? setFocusedElement = keyboard?.GetType().GetMethod(
            "SetFocusedElement",
            new[] { typeof(IInputElement), typeof(NavigationMethod), typeof(KeyModifiers) });
        setFocusedElement?.Invoke(keyboard, new object?[] { null, NavigationMethod.Unspecified, KeyModifiers.None });
    }

    private static void ExecuteLayoutPass(Window window)
    {
        PropertyInfo? layoutProperty = null;
        for (Type? type = window.GetType(); type is not null && layoutProperty is null; type = type.BaseType)
        {
            layoutProperty = type.GetProperty(
                "LayoutManager",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        object? layoutManager = layoutProperty?.GetValue(window);
        MethodInfo? executeMethod = layoutManager?.GetType().GetMethod(
            "ExecuteLayoutPass",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        executeMethod?.Invoke(layoutManager, null);
    }
}
