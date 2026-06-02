namespace ProTranslate.MewUI.Tests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ReleaseFactAttribute : FactAttribute
{
    public ReleaseFactAttribute()
    {
#if DEBUG
        Skip = "Leak tests run in Release configuration to avoid Debug JIT local-root false positives.";
#endif
    }
}
