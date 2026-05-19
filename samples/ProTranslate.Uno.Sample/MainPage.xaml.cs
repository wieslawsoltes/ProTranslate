using Microsoft.UI.Xaml.Controls;
using ProTranslate.Samples.Shared;

namespace ProTranslate.Uno.Sample;

public sealed partial class MainPage : Page
{
    public MainPage()
        : this(SampleTranslations.Create())
    {
    }

    public MainPage(SampleTranslationHost host)
    {
        ProTranslate.Uno.TranslationService.UseService(host.Translations, host.Cultures);
        ViewModel = new TranslationDemoViewModel(host);
        InitializeComponent();
    }

    public TranslationDemoViewModel ViewModel { get; }
}
