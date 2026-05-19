using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using Xunit;

namespace ProTranslate.Avalonia.Tests;

[Collection("AvaloniaHeadless")]
public sealed class AvaloniaRuntimeUiTests
{
    [Fact]
    public async Task AttachedTextBlockRefreshesTranslatedTextWhenCultureChanges()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));

        await session.Dispatch(() =>
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

            return Task.CompletedTask;
        }, CancellationToken.None);
    }

    [Fact]
    public async Task FormatExtensionBindingRefreshesTextWhenValueChanges()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));

        await session.Dispatch(() =>
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

            return Task.CompletedTask;
        }, CancellationToken.None);
    }

    [Fact]
    public async Task AttachedTranslationKeyRefreshesContentWhenCultureChanges()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));

        await session.Dispatch(() =>
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

            return Task.CompletedTask;
        }, CancellationToken.None);
    }

    [Fact]
    public async Task AutoFlowDirectionRefreshesWhenAttachedCultureChanges()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));

        await session.Dispatch(() =>
        {
            UseTranslations(("en-US", "Shell.Title", "Orders"));
            var grid = new Grid();

            Translation.SetAutoFlowDirection(grid, true);
            Translation.SetCulture(grid, CultureInfo.GetCultureInfo("en-US"));

            Assert.Equal(FlowDirection.LeftToRight, grid.GetValue(Visual.FlowDirectionProperty));

            Translation.SetCulture(grid, CultureInfo.GetCultureInfo("ar-SA"));

            Assert.Equal(FlowDirection.RightToLeft, grid.GetValue(Visual.FlowDirectionProperty));

            return Task.CompletedTask;
        }, CancellationToken.None);
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
