using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using ProTranslate.Avalonia;
using Xunit;

namespace ProTranslate.Avalonia.Tests;

public sealed class AvaloniaAdapterSmokeTests
{
    [Fact]
    public void TranslateExtensionReturnsOneWayBindingToTranslationSource()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Title", "ProTranslate");
        var service = new global::ProTranslate.TranslationService(provider, cultures);
        ProTranslate.Avalonia.TranslationService.UseService(service, cultures);

        var binding = Assert.IsType<Binding>(new TranslateExtension("Shell.Title").ProvideValue(new EmptyServiceProvider()));

        Assert.Equal("[Shell.Title]", binding.Path);
        Assert.Same(ProTranslate.Avalonia.TranslationService.Source, binding.Source);
        Assert.Equal(BindingMode.OneWay, binding.Mode);
        Assert.Equal("ProTranslate", ProTranslate.Avalonia.TranslationService.Source["Shell.Title"]);
    }

    [Fact]
    public void FormatExtensionReturnsBindingThroughTranslationSource()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Orders.Total", "Total: {0:C}");
        var service = new global::ProTranslate.TranslationService(provider, cultures);
        ProTranslate.Avalonia.TranslationService.UseService(service, cultures);

        var binding = Assert.IsType<MultiBinding>(new FormatExtension("Orders.Total")
        {
            Value = 12.5m,
            FallbackValue = "n/a"
        }.ProvideValue(new EmptyServiceProvider()));

        Assert.Equal("n/a", binding.FallbackValue);
        Assert.NotNull(binding.Converter);
        Assert.Equal(2, binding.Bindings.Count);
        Assert.Equal("Total: $12.50", ProTranslate.Avalonia.TranslationService.Source.Translate("Orders.Total", 12.5m));
    }

    [Fact]
    public void AttachedKeyUpdatesTextBlockWhenCultureChanges()
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Title", "Title")
            .Add(CultureInfo.GetCultureInfo("pl-PL"), "Shell.Title", "Tytul");
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = new global::ProTranslate.TranslationService(provider, cultures);
        ProTranslate.Avalonia.TranslationService.UseService(service, cultures);
        var textBlock = new TextBlock();

        Translation.SetKey(textBlock, "Shell.Title");

        Assert.Equal("Title", textBlock.Text);

        cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));

        Assert.Equal("Tytul", textBlock.Text);
    }

    [Fact]
    public void UseServiceKeepsExistingBindingSourceAlive()
    {
        var firstCultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var firstProvider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Title", "First");
        var firstService = new global::ProTranslate.TranslationService(firstProvider, firstCultures);
        ProTranslate.Avalonia.TranslationService.UseService(firstService, firstCultures);

        var binding = Assert.IsType<Binding>(new TranslateExtension("Shell.Title").ProvideValue(new EmptyServiceProvider()));
        var capturedSource = Assert.IsType<TranslationBindingSource>(binding.Source);

        var secondCultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var secondProvider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Title", "Second");
        var secondService = new global::ProTranslate.TranslationService(secondProvider, secondCultures);
        ProTranslate.Avalonia.TranslationService.UseService(secondService, secondCultures);

        Assert.Same(capturedSource, ProTranslate.Avalonia.TranslationService.Source);
        Assert.Equal("Second", capturedSource["Shell.Title"]);
    }

    [Fact]
    public void SourceRaisesIndexerChangeWhenCultureChanges()
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Hello", "Hello:en-US")
            .Add(CultureInfo.GetCultureInfo("pl-PL"), "Hello", "Hello:pl-PL");
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = new global::ProTranslate.TranslationService(provider, cultures);
        var source = new TranslationBindingSource(service, cultures);
        var raised = new List<string?>();
        source.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));

        Assert.Equal("Hello:pl-PL", source["Hello"]);
        Assert.Contains(nameof(TranslationBindingSource.Culture), raised);
        Assert.Contains("Item[]", raised);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
