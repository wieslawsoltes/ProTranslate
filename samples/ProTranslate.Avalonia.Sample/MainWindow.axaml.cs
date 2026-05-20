using Avalonia.Controls;
using ProTranslate.Samples.Shared;

namespace ProTranslate.Avalonia.Sample;

public sealed partial class MainWindow : Window
{
    public MainWindow()
        : this(SampleTranslations.Create())
    {
    }

    public MainWindow(SampleTranslationHost host)
    {
        ProTranslate.Avalonia.TranslationService.UseService(host.Translations, host.Cultures);
        InitializeComponent();
        DataContext = new TranslationDemoViewModel(host);
    }
}
