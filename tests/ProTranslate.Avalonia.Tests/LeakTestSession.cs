using Avalonia.Headless;

namespace ProTranslate.Avalonia.Tests;

internal static class LeakTestSession
{
    internal static T RunInSession<T>(Func<T> action)
    {
        HeadlessUnitTestSession? session = null;
        try
        {
            session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));
            return session.Dispatch(action, CancellationToken.None).GetAwaiter().GetResult();
        }
        finally
        {
            session?.Dispose();
            LeakTestHelpers.ResetHeadlessCompositor();
            LeakTestHelpers.ResetLoadedQueueForUnitTests();
            LeakTestHelpers.StopDispatcherTimers();
        }
    }
}

