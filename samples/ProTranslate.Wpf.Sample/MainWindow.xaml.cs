using System.Windows;
using ProTranslate.Samples.Shared;

namespace ProTranslate.Wpf.Sample;

public partial class MainWindow : Window
{
    public MainWindow()
        : this(SampleTranslations.Create())
    {
    }

    public MainWindow(SampleTranslationHost host)
    {
        ProTranslate.Wpf.TranslationService.UseService(host.Translations, host.Cultures);
        InitializeComponent();
        DataContext = new TranslationDemoViewModel(host);
    }
}
