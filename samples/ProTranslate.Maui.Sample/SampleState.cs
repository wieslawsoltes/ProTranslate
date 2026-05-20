using ProTranslate.Samples.Shared;

namespace ProTranslate.Maui.Sample;

internal static class SampleState
{
    public static SampleTranslationHost Host { get; set; } = SampleTranslations.Create();
}
