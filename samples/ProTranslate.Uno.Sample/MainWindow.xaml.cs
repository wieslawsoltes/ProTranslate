using Microsoft.UI.Xaml;
using ProTranslate.Samples.Shared;

namespace ProTranslate.Uno.Sample;

public sealed partial class MainWindow : Window
{
    public MainWindow()
        : this(SampleTranslations.Create())
    {
    }

    public MainWindow(SampleTranslationHost host)
    {
        ProTranslate.Uno.TranslationService.UseService(host.Translations, host.Cultures);
        ViewModel = new TranslationDemoViewModel(host);
        InitializeComponent();
    }

    public TranslationDemoViewModel ViewModel { get; }
}
