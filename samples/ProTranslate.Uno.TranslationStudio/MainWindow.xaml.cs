using Microsoft.UI.Xaml;

namespace ProTranslate.Uno.TranslationStudio;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        ViewModel = new TranslationStudioViewModel();
        InitializeComponent();
    }

    public TranslationStudioViewModel ViewModel { get; }
}
