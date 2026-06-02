using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

using ProTranslate.MewUI;
using ProTranslate.Samples.Shared;

// ── Register the MewUI platform + rendering backend for the current OS ──────────
if (OperatingSystem.IsMacOS())
{
    MacOSPlatform.Register();
    MewVGMacOSBackend.Register();
}
else if (OperatingSystem.IsWindows())
{
    Win32Platform.Register();
    GdiBackend.Register();
}
else
{
    throw new PlatformNotSupportedException("This sample supports Windows and macOS.");
}

// ── Wire ProTranslate into the MewUI adapter (shared source-generated catalog) ──
var host = SampleTranslations.Create();
TranslationService.UseService(host.Translations, host.Cultures);

var cultures = new[]
{
    new CultureChoice("en-US", "US", "English"),
    new CultureChoice("pl-PL", "PL", "Polski"),
    new CultureChoice("ar-SA", "SA", "العربية"),
};

// A formatted string that re-evaluates whenever the culture changes.
var greeting = new ObservableValue<string>(
    host.Translations.Format("GreetingFormat", "Ada"));
host.Translations.CultureChanged += (_, _) =>
    greeting.Value = host.Translations.Format("GreetingFormat", "Ada");

Application.Create()
    .BuildMainWindow(BuildWindow)
    .Run();

Window BuildWindow()
{
    var window = new Window()
        .Resizable(560, 420)
        .StartCenterScreen();

    window.TranslateTitle("AppTitle");

    var languagePicker = new ComboBox()
        .Width(200)
        .Items(cultures, c => c.DisplayName, c => c.CultureName)
        .SelectedIndex(0)
        .OnSelectionChanged(selected =>
        {
            if (selected is CultureChoice choice)
                TranslationService.Culture =
                    System.Globalization.CultureInfo.GetCultureInfo(choice.CultureName);
        });

    window.Content = new ScrollViewer()
        .AutoVerticalScroll()
        .Content(
            new StackPanel()
                .Vertical()
                .Padding(28)
                .Spacing(16)
                .Children(
                    // Title + subtitle bound through the fluent adapter API
                    new TextBlock()
                        .Bind(TextBlock.TextProperty, TranslationExtensions.TranslationBinding("AppTitle"))
                        .FontSize(22)
                        .Bold(),
                    new TextBlock()
                        .Bind(TextBlock.TextProperty, TranslationExtensions.TranslationBinding("FrameworkSubtitle"))
                        .TextWrapping(TextWrapping.Wrap)
                        .WithTheme((t, c) => c.Foreground(t.Palette.DisabledText)),

                    // Live culture switch
                    new TextBlock()
                        .Bind(TextBlock.TextProperty, TranslationExtensions.TranslationBinding("LiveSwitchLabel"))
                        .SemiBold()
                        .Margin(0, 12, 0, 0),
                    languagePicker,

                    // A formatted, culture-aware string
                    new TextBlock()
                        .Bind(TextBlock.TextProperty, TranslationExtensions.TranslationBinding("OrderTitle"))
                        .SemiBold()
                        .Margin(0, 12, 0, 0),
                    new TextBlock()
                        .Bind(TextBlock.TextProperty, greeting)
                        .TextWrapping(TextWrapping.Wrap)));

    return window;
}
