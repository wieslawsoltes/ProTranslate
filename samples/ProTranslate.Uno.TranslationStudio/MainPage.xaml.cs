using Microsoft.UI.Xaml.Controls;

namespace ProTranslate.Uno.TranslationStudio;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        ViewModel = new TranslationStudioViewModel();
        InitializeComponent();
    }

    public TranslationStudioViewModel ViewModel { get; }
}
