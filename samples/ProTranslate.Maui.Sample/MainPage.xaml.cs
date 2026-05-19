using ProTranslate.Samples.Shared;

namespace ProTranslate.Maui.Sample;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new TranslationDemoViewModel(SampleState.Host);
    }
}
