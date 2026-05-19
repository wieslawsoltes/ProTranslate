using Xunit;

namespace ProTranslate.Avalonia.Tests;

public sealed class ReleaseFactAttribute : FactAttribute
{
    public ReleaseFactAttribute()
    {
#if !RELEASE
        Skip = "Leak tests run in Release configuration to avoid Debug JIT local-root false positives.";
#endif
    }
}
