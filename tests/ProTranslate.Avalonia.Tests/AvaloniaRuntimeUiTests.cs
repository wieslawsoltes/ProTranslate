using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Xunit;

namespace ProTranslate.Avalonia.Tests;

[Collection("AvaloniaHeadless")]
public sealed class AvaloniaRuntimeUiTests
{
    [AvaloniaFact]
    public void AttachedTextBlockRefreshesTranslatedTextWhenCultureChanges()
    {
        var cultures = UseTranslations(
            ("en-US", "Shell.Title", "Orders"),
            ("pl-PL", "Shell.Title", "Zamowienia"));
        var textBlock = new TextBlock();

        Translation.SetKey(textBlock, "Shell.Title");

        WithWindow(textBlock, () =>
        {
            DrainUi();
            Assert.Equal("Orders", textBlock.Text);

            cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));
            DrainUi();

            Assert.Equal("Zamowienia", textBlock.Text);
        });
    }

    [AvaloniaFact]
    public void FormatExtensionBindingRefreshesTextWhenValueChanges()
    {
        var cultures = UseTranslations(
            ("en-US", "Orders.Total", "Total: {0}"),
            ("pl-PL", "Orders.Total", "Suma: {0}"));
        var viewModel = new OrderViewModel { Total = 12 };
        var textBlock = new TextBlock
        {
            DataContext = viewModel
        };
        textBlock.Bind(
            TextBlock.TextProperty,
            Assert.IsType<MultiBinding>(new FormatExtension("Orders.Total")
            {
                Value = new Binding(nameof(OrderViewModel.Total))
            }.ProvideValue(new EmptyServiceProvider())));

        WithWindow(textBlock, () =>
        {
            DrainUi();
            Assert.Equal("Total: 12", textBlock.Text);

            viewModel.Total = 15;
            DrainUi();

            Assert.Equal("Total: 15", textBlock.Text);

            cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));
            viewModel.Total = 16;
            DrainUi();

            Assert.Equal("Suma: 16", textBlock.Text);
        });
    }

    [AvaloniaFact]
    public void AttachedTranslationKeyRefreshesContentWhenCultureChanges()
    {
        var cultures = UseTranslations(
            ("en-US", "Orders.EmptyState", "No orders"),
            ("pl-PL", "Orders.EmptyState", "Brak zamowien"));
        var contentControl = new ContentControl();

        Translation.SetKey(contentControl, "Orders.EmptyState");

        WithWindow(contentControl, () =>
        {
            DrainUi();
            Assert.Equal("No orders", contentControl.Content);

            cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));
            DrainUi();

            Assert.Equal("Brak zamowien", contentControl.Content);
        });
    }

    [AvaloniaFact]
    public void AutoFlowDirectionRefreshesWhenAttachedCultureChanges()
    {
        UseTranslations(("en-US", "Shell.Title", "Orders"));
        var grid = new Grid();

        Translation.SetAutoFlowDirection(grid, true);
        Translation.SetCulture(grid, CultureInfo.GetCultureInfo("en-US"));

        Assert.Equal(FlowDirection.LeftToRight, grid.GetValue(Visual.FlowDirectionProperty));

        Translation.SetCulture(grid, CultureInfo.GetCultureInfo("ar-SA"));

        Assert.Equal(FlowDirection.RightToLeft, grid.GetValue(Visual.FlowDirectionProperty));
    }

    [AvaloniaFact]
    public void AutoFlowDirectionRefreshesWhenSourceCultureChanges()
    {
        var cultures = UseTranslations(("en-US", "Shell.Title", "Orders"));
        var grid = new Grid();

        Translation.SetAutoFlowDirection(grid, true);
        WithWindow(grid, () =>
        {
            DrainUi();
            Assert.Equal(FlowDirection.LeftToRight, grid.GetValue(Visual.FlowDirectionProperty));

            cultures.SetCulture(CultureInfo.GetCultureInfo("ar-SA"));
            DrainUi();

            Assert.Equal(FlowDirection.RightToLeft, grid.GetValue(Visual.FlowDirectionProperty));
        });
    }

    [AvaloniaFact]
    public void AttachedTargetFollowsReplacementTranslationSource()
    {
        UseTranslations(("en-US", "Shell.Title", "First"));
        var textBlock = new TextBlock();

        Translation.SetKey(textBlock, "Shell.Title");

        WithWindow(textBlock, () =>
        {
            DrainUi();
            Assert.Equal("First", textBlock.Text);

            UseTranslations(("en-US", "Shell.Title", "Second"));
            DrainUi();

            Assert.Equal("Second", textBlock.Text);
        });
    }

    [AvaloniaFact]
    public async Task AttachedTargetRefreshesWhenCultureChangesOffUiThread()
    {
        var cultures = UseTranslations(
            ("en-US", "Shell.Title", "Orders"),
            ("pl-PL", "Shell.Title", "Zamowienia"));
        var textBlock = new TextBlock();

        Translation.SetKey(textBlock, "Shell.Title");
        var window = new Window
        {
            Width = 360,
            Height = 160,
            Content = textBlock
        };

        try
        {
            window.Show();
            DrainUi();
            Assert.Equal("Orders", textBlock.Text);

            await Task.Run(() => cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL")));
            DrainUi();

            Assert.Equal("Zamowienia", textBlock.Text);
        }
        finally
        {
            Translation.SetKey(textBlock, null);
            window.Content = null;
            window.Close();
            DrainUi();
        }
    }

    private static CultureService UseTranslations(params (string Culture, string Key, string Value)[] values)
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var provider = new InMemoryTranslationProvider();

        foreach ((string culture, string key, string value) in values)
        {
            provider.Add(CultureInfo.GetCultureInfo(culture), key, value);
        }

        var service = new global::ProTranslate.TranslationService(provider, cultures);
        ProTranslate.Avalonia.TranslationService.UseSource(new TranslationBindingSource(service, cultures));
        return cultures;
    }

    private static void WithWindow(Control content, Action assertions)
    {
        var window = new Window
        {
            Width = 360,
            Height = 160,
            Content = content
        };

        try
        {
            window.Show();
            DrainUi();
            assertions();
        }
        finally
        {
            Translation.SetKey(content, null);
            window.Content = null;
            window.Close();
            DrainUi();
        }
    }

    private static void DrainUi()
    {
        Dispatcher.UIThread.RunJobs();
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }

    private sealed class OrderViewModel : INotifyPropertyChanged
    {
        private int _total;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Total
        {
            get => _total;
            set
            {
                if (_total == value)
                {
                    return;
                }

                _total = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Total)));
            }
        }
    }
}
