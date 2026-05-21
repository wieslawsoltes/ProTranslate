namespace ProTranslate.Uno.Tests;

internal static class LeakTestHelpers
{
    internal static void AssertCollected(params WeakReference[] references)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

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
}

