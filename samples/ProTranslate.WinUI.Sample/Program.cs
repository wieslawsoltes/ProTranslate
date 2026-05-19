using Microsoft.UI.Xaml;

namespace ProTranslate.WinUI.Sample;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Application.Start(_ => _ = new App());
    }
}
