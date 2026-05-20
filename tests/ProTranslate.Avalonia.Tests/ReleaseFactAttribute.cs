using System.Runtime.CompilerServices;
using Xunit;

namespace ProTranslate.Avalonia.Tests;

public sealed class ReleaseFactAttribute : FactAttribute
{
    public ReleaseFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
#if !RELEASE
        Skip = "Leak tests run in Release configuration to avoid Debug JIT local-root false positives.";
#endif
    }
}
