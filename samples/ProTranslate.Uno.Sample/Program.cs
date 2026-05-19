using Microsoft.UI.Xaml;

namespace ProTranslate.Uno.Sample;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Application.Start(_ => _ = new App());
    }
}
