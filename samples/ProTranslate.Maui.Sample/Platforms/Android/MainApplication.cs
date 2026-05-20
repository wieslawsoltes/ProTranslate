using Android.App;
using Android.Runtime;
using Microsoft.Maui;

namespace ProTranslate.Maui.Sample;

[Application]
public sealed class MainApplication : MauiApplication
{
    public MainApplication(nint handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp()
    {
        return MauiProgram.CreateMauiApp();
    }
}
