using System.Globalization;
using System.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ProTranslate.Tests;

public sealed class CoreTranslationTests
{
    [Fact]
    public void TranslationServiceFallsBackFromSpecificCultureToParentCulture()
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("pl"), "Shell.Save", "Zapisz");
        var cultures = new CultureService(CultureInfo.GetCultureInfo("pl-PL"));
        var service = new TranslationService(provider, cultures);

        LocalizedString value = service.GetString("Shell.Save");

        Assert.Equal("Zapisz", value.Value);
        Assert.Equal("pl", value.Culture.Name);
        Assert.False(value.ResourceNotFound);
    }

    [Fact]
    public void TranslationServiceReturnsKeyForMissingResource()
    {
        var provider = new InMemoryTranslationProvider();
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = new TranslationService(provider, cultures);
        var diagnostics = new List<ProTranslateDiagnostic>();
        service.DiagnosticReported += (_, args) => diagnostics.Add(args.Diagnostic);

        LocalizedString value = service.GetString("Missing.Key");

        Assert.Equal("Missing.Key", value.Value);
        Assert.True(value.ResourceNotFound);
        Assert.Single(diagnostics);
        Assert.Equal(ProTranslateDiagnosticKind.MissingTranslation, diagnostics[0].Kind);
        Assert.Single(value.Diagnostics);
    }

    [Fact]
    public void TranslationServiceTreatsEmptyStringAsIntentionalValue()
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Empty.Value", string.Empty);
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = new TranslationService(provider, cultures);
        var diagnostics = new List<ProTranslateDiagnostic>();
        service.DiagnosticReported += (_, args) => diagnostics.Add(args.Diagnostic);

        LocalizedString value = service.GetString("Empty.Value");

        Assert.Equal(string.Empty, value.Value);
        Assert.False(value.ResourceNotFound);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void TranslationServiceReportsProviderExceptionAndContinuesFallbackLookup()
    {
        var broken = new ThrowingTranslationProvider("Broken");
        var fallback = new InMemoryTranslationProvider("Fallback")
            .Add(CultureInfo.GetCultureInfo("en-US"), "Shell.Open", "Open");
        var provider = new CompositeTranslationProvider(broken, fallback);
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = new TranslationService(provider, cultures);
        var diagnostics = new List<ProTranslateDiagnostic>();
        service.DiagnosticReported += (_, args) => diagnostics.Add(args.Diagnostic);

        LocalizedString value = service.GetString("Shell.Open");

        Assert.Equal("Open", value.Value);
        Assert.False(value.ResourceNotFound);
        ProTranslateDiagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(ProTranslateDiagnosticKind.ProviderFailure, diagnostic.Kind);
        Assert.Equal("Broken", diagnostic.ProviderName);
    }

    [Fact]
    public void TranslationServiceCanThrowProviderExceptionAfterReportingDiagnostic()
    {
        var provider = new ThrowingTranslationProvider("Broken");
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var options = new TranslationFallbackOptions
        {
            ProviderFailureBehavior = TranslationProviderFailureBehavior.ReportAndThrow
        };
        var service = new TranslationService(provider, cultures, options);
        var diagnostics = new List<ProTranslateDiagnostic>();
        service.DiagnosticReported += (_, args) => diagnostics.Add(args.Diagnostic);

        Assert.Throws<InvalidOperationException>(() => service.GetString("Shell.Open"));
        ProTranslateDiagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(ProTranslateDiagnosticKind.ProviderFailure, diagnostic.Kind);
        Assert.Equal("Broken", diagnostic.ProviderName);
    }

    [Fact]
    public void TranslationServiceUsesUiCultureForLookupAndFormattingCultureForFormat()
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Orders.Total", "{0:N2}");
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"), CultureInfo.GetCultureInfo("en-US"));
        var service = new TranslationService(provider, cultures);

        LocalizedString localized = service.GetString("Orders.Total");
        string formatted = service.Format("Orders.Total", 1234.5m);

        Assert.Equal("en-US", localized.Culture.Name);
        Assert.Equal(string.Format(CultureInfo.GetCultureInfo("pl-PL"), "{0:N2}", 1234.5m), formatted);
    }

    [Fact]
    public void TranslationServiceReportsFormattingFailures()
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Broken.Format", "{0:0.00");
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = new TranslationService(provider, cultures);
        var diagnostics = new List<ProTranslateDiagnostic>();
        service.DiagnosticReported += (_, args) => diagnostics.Add(args.Diagnostic);

        string value = service.Format("Broken.Format", 42);

        Assert.Equal("{0:0.00", value);
        ProTranslateDiagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(ProTranslateDiagnosticKind.FormatFailure, diagnostic.Kind);
    }

    [Fact]
    public void TranslationServiceCanThrowFormattingFailuresAfterReportingDiagnostic()
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Broken.Format", "{0:0.00");
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var options = new TranslationFallbackOptions
        {
            FormatFailureBehavior = TranslationFormatFailureBehavior.ReportAndThrow
        };
        var service = new TranslationService(provider, cultures, options);
        var diagnostics = new List<ProTranslateDiagnostic>();
        service.DiagnosticReported += (_, args) => diagnostics.Add(args.Diagnostic);

        Assert.Throws<FormatException>(() => service.Format("Broken.Format", 42));
        ProTranslateDiagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(ProTranslateDiagnosticKind.FormatFailure, diagnostic.Kind);
    }

    [Fact]
    public void CultureServiceUpdatesCurrentAndThreadCultures()
    {
        CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
        CultureInfo originalUICulture = Thread.CurrentThread.CurrentUICulture;
        CultureInfo? originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        CultureInfo? originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

        try
        {
            var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
            var events = new List<CultureChangedEventArgs>();
            cultures.CultureChanged += (_, args) => events.Add(args);

            cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));

            Assert.Equal("pl-PL", cultures.CurrentCulture.Name);
            Assert.Equal("pl-PL", cultures.CurrentUICulture.Name);
            Assert.Equal("pl-PL", CultureInfo.CurrentCulture.Name);
            Assert.Equal("pl-PL", CultureInfo.CurrentUICulture.Name);
            Assert.Single(events);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUICulture;
            CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUICulture;
        }
    }

    [Fact]
    public void CultureServiceConstructionDoesNotMutateThreadCultures()
    {
        CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
        CultureInfo originalUICulture = Thread.CurrentThread.CurrentUICulture;
        CultureInfo? originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        CultureInfo? originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

        try
        {
            var cultures = new CultureService(CultureInfo.GetCultureInfo("fr-FR"));

            Assert.Equal("fr-FR", cultures.CurrentCulture.Name);
            Assert.Equal("fr-FR", cultures.CurrentUICulture.Name);
            Assert.Equal(originalCulture, Thread.CurrentThread.CurrentCulture);
            Assert.Equal(originalUICulture, Thread.CurrentThread.CurrentUICulture);
            Assert.Equal(originalDefaultCulture, CultureInfo.DefaultThreadCurrentCulture);
            Assert.Equal(originalDefaultUICulture, CultureInfo.DefaultThreadCurrentUICulture);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUICulture;
            CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUICulture;
        }
    }

    [Fact]
    public void CultureServiceCanUseSnapshotOnlyModeWithoutMutatingThreadCultures()
    {
        CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
        CultureInfo originalUICulture = Thread.CurrentThread.CurrentUICulture;
        CultureInfo? originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        CultureInfo? originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

        try
        {
            var options = new CultureServiceOptions
            {
                ApplyToCurrentThread = false,
                ApplyToDefaultThread = false
            };
            var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"), options);

            cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));

            Assert.Equal("pl-PL", cultures.CurrentCulture.Name);
            Assert.Equal("pl-PL", cultures.CurrentUICulture.Name);
            Assert.Equal(originalCulture, Thread.CurrentThread.CurrentCulture);
            Assert.Equal(originalUICulture, Thread.CurrentThread.CurrentUICulture);
            Assert.Equal(originalDefaultCulture, CultureInfo.DefaultThreadCurrentCulture);
            Assert.Equal(originalDefaultUICulture, CultureInfo.DefaultThreadCurrentUICulture);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUICulture;
            CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUICulture;
        }
    }

    [Fact]
    public void CultureChangedEventCarriesFormattingAndUiCultures()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        CultureChangedEventArgs? changed = null;
        cultures.CultureChanged += (_, args) => changed = args;

        cultures.SetCulture(CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("ar-SA"));

        Assert.NotNull(changed);
        Assert.Equal("en-US", changed.OldCulture.Name);
        Assert.Equal("en-US", changed.OldUICulture.Name);
        Assert.Equal("en-US", changed.NewCulture.Name);
        Assert.Equal("ar-SA", changed.NewUICulture.Name);
        Assert.Equal(TextFlowDirection.RightToLeft, cultures.FlowDirection);
        Assert.Equal("US", cultures.CurrentRegion.TwoLetterISORegionName);
    }

    [Fact]
    public void ObservableLocalizedStringRaisesValueChangeWhenCultureChanges()
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Greeting", "Hello {0}")
            .Add(CultureInfo.GetCultureInfo("pl-PL"), "Greeting", "Czesc {0}");
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = new TranslationService(provider, cultures);
        using IObservableLocalizedString text = service.Observe("Greeting", "Marta");
        var changed = new List<string?>();
        text.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));

        Assert.Equal("Czesc Marta", text.Value);
        Assert.Contains(nameof(IObservableLocalizedString.Value), changed);
    }

    [Fact]
    public void TranslationServiceCacheReducesProviderCalls()
    {
        var inner = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Greeting", "Hello");
        var provider = new CountingTranslationProvider(inner);
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = new TranslationService(provider, cultures);

        LocalizedString first = service.GetString("Greeting");
        LocalizedString second = service.GetString("Greeting");

        Assert.Equal("Hello", first.Value);
        Assert.Equal("Hello", second.Value);
        Assert.Equal(1, provider.CallCount);
        Assert.Equal(1, service.CachedLookupCount);
    }

    [Fact]
    public void TranslationServiceCacheKeepsCultureSpecificValuesCorrect()
    {
        var inner = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Greeting", "Hello")
            .Add(CultureInfo.GetCultureInfo("pl-PL"), "Greeting", "Czesc");
        var provider = new CountingTranslationProvider(inner);
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = new TranslationService(provider, cultures);

        LocalizedString english = service.GetString("Greeting");
        cultures.SetCulture(CultureInfo.GetCultureInfo("pl-PL"));
        LocalizedString polish = service.GetString("Greeting");
        LocalizedString cachedPolish = service.GetString("Greeting");

        Assert.Equal("Hello", english.Value);
        Assert.Equal("Czesc", polish.Value);
        Assert.Equal("Czesc", cachedPolish.Value);
        Assert.Equal(2, provider.CallCount);
    }

    [Fact]
    public void TranslationServiceInvalidatesCacheWhenFallbackPolicyChanges()
    {
        var inner = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Greeting", "Hello");
        var provider = new CountingTranslationProvider(inner);
        var cultures = new CultureService(CultureInfo.GetCultureInfo("fr-FR"));
        var options = new TranslationFallbackOptions
        {
            UseParentCultures = false,
            UseDefaultCulture = false
        };
        var service = new TranslationService(provider, cultures, options);

        LocalizedString missing = service.GetString("Greeting");
        options.FallbackCultures.Add(CultureInfo.GetCultureInfo("en-US"));
        LocalizedString resolved = service.GetString("Greeting");

        Assert.True(missing.ResourceNotFound);
        Assert.Equal("Hello", resolved.Value);
        Assert.False(resolved.ResourceNotFound);
        Assert.True(provider.CallCount >= 2);
        Assert.Equal(1, service.CachedLookupCount);
    }

    [Fact]
    public void TranslationServiceAllowsExplicitCacheInvalidationForProviderChanges()
    {
        var provider = new InMemoryTranslationProvider()
            .Add(CultureInfo.GetCultureInfo("en-US"), "Greeting", "Hello");
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var service = new TranslationService(provider, cultures);

        Assert.Equal("Hello", service.GetString("Greeting").Value);

        provider.Add(CultureInfo.GetCultureInfo("en-US"), "Greeting", "Hi");
        Assert.Equal("Hello", service.GetString("Greeting").Value);

        Assert.True(service.RemoveCachedString("Greeting", CultureInfo.GetCultureInfo("en-US")));
        Assert.Equal("Hi", service.GetString("Greeting").Value);
    }

    [Theory]
    [InlineData("ar-SA", TextFlowDirection.RightToLeft)]
    [InlineData("he-IL", TextFlowDirection.RightToLeft)]
    [InlineData("pl-PL", TextFlowDirection.LeftToRight)]
    public void CultureServiceResolvesFlowDirection(string cultureName, TextFlowDirection expected)
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal(expected, cultures.FlowDirection);
    }

    [Theory]
    [InlineData("en-US", MeasurementSystem.USCustomary)]
    [InlineData("pl-PL", MeasurementSystem.Metric)]
    [InlineData("en-GB", MeasurementSystem.Imperial)]
    public void GlobalizationServiceResolvesMeasurementSystem(string cultureName, MeasurementSystem expected)
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo(cultureName));
        var translations = new TranslationService(new InMemoryTranslationProvider(), cultures);
        var globalization = new GlobalizationService(cultures, translations);

        Assert.Equal(expected, globalization.MeasurementSystem);
    }

    [Fact]
    public void RegionProfileProviderResolvesSpecificRegionMetadata()
    {
        var provider = new DefaultRegionProfileProvider();

        RegionProfile profile = provider.GetProfile(CultureInfo.GetCultureInfo("fr-CA"));

        Assert.Equal("CA", profile.TwoLetterISORegionName);
        Assert.Equal("CAD", profile.ISOCurrencySymbol);
        Assert.True(profile.IsMetric);
    }

    [Fact]
    public void GlobalizationServiceHonorsRegionAndMeasurementOverrides()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("pl-PL"));
        var translations = new TranslationService(new InMemoryTranslationProvider(), cultures);
        var globalization = new GlobalizationService(cultures, translations);

        globalization.SetRegionOverride(new RegionInfo("US"));

        Assert.Equal("US", globalization.RegionProfile.TwoLetterISORegionName);
        Assert.Equal(MeasurementSystem.USCustomary, globalization.MeasurementSystem);
        Assert.False(globalization.IsMetric);

        globalization.SetMeasurementSystemOverride(MeasurementSystem.Metric);
        cultures.SetCulture(CultureInfo.GetCultureInfo("ja-JP"));

        Assert.Equal("US", globalization.RegionProfile.TwoLetterISORegionName);
        Assert.Equal(MeasurementSystem.Metric, globalization.MeasurementSystem);
        Assert.True(globalization.IsMetric);
        Assert.Equal("m", globalization.MeasurementSystemProfile.LengthUnit);

        globalization.ClearRegionOverride();
        globalization.ClearMeasurementSystemOverride();

        Assert.Equal("JP", globalization.RegionProfile.TwoLetterISORegionName);
        Assert.Equal(MeasurementSystem.Metric, globalization.MeasurementSystem);
    }

    [Theory]
    [InlineData(1d, MeasurementUnit.Kilometer, MeasurementUnit.Meter, 1000d)]
    [InlineData(32d, MeasurementUnit.Fahrenheit, MeasurementUnit.Celsius, 0d)]
    [InlineData(1d, MeasurementUnit.Pound, MeasurementUnit.Kilogram, 0.45359237d)]
    [InlineData(1d, MeasurementUnit.USGallon, MeasurementUnit.Liter, 3.785411784d)]
    [InlineData(1d, MeasurementUnit.ImperialGallon, MeasurementUnit.Liter, 4.54609d)]
    [InlineData(60d, MeasurementUnit.MilesPerHour, MeasurementUnit.MetersPerSecond, 26.8224d)]
    public void UnitConversionServiceConvertsSupportedCategories(
        double value,
        MeasurementUnit fromUnit,
        MeasurementUnit toUnit,
        double expected)
    {
        var converter = new DefaultUnitConversionService();

        double actual = converter.Convert(value, fromUnit, toUnit);

        Assert.Equal(expected, actual, precision: 6);
    }

    [Fact]
    public void UnitConversionServiceConvertsToMeasurementProfiles()
    {
        var converter = new DefaultUnitConversionService();
        var resolver = new DefaultMeasurementSystemResolver();
        var usProfile = resolver.Resolve(new DefaultRegionProfileProvider()
            .GetProfile(CultureInfo.GetCultureInfo("en-US")));
        var imperialProfile = resolver.Resolve(new DefaultRegionProfileProvider()
            .GetProfile(CultureInfo.GetCultureInfo("en-GB")));

        MeasurementValue usLength = converter.ConvertToProfile(1d, MeasurementUnit.Meter, usProfile);
        MeasurementValue imperialVolume = converter.ConvertToProfile(1d, MeasurementUnit.Liter, imperialProfile);

        Assert.Equal(MeasurementUnit.Foot, usLength.Unit);
        Assert.Equal(3.280839895, usLength.Value, precision: 6);
        Assert.Equal(MeasurementUnit.ImperialGallon, imperialVolume.Unit);
        Assert.Equal(0.219969248, imperialVolume.Value, precision: 6);
    }

    [Fact]
    public void LocalizedUnitFormatterUsesCultureNumberFormatting()
    {
        var formatter = new DefaultLocalizedUnitFormatter();
        CultureInfo culture = CultureInfo.GetCultureInfo("pl-PL");

        string formatted = formatter.Format(1234.5d, MeasurementUnit.Meter, culture, "N1");

        Assert.Equal(string.Concat(1234.5d.ToString("N1", culture), " m"), formatted);
        Assert.Contains(culture.NumberFormat.NumberDecimalSeparator, formatted);
    }

    [Fact]
    public void GlobalizationServiceFormatsMeasurementsForActiveProfile()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("en-US"));
        var translations = new TranslationService(new InMemoryTranslationProvider(), cultures);
        var globalization = new GlobalizationService(cultures, translations);

        string formatted = globalization.FormatMeasurementForProfile(1d, MeasurementUnit.Kilometer, "N1");

        Assert.Equal(
            string.Concat(3280.839895013123d.ToString("N1", CultureInfo.GetCultureInfo("en-US")), " ft"),
            formatted);
    }

    [Fact]
    public void GlobalizationServiceRaisesChangeEventsForPreferenceOverrides()
    {
        var cultures = new CultureService(CultureInfo.GetCultureInfo("pl-PL"));
        var translations = new TranslationService(new InMemoryTranslationProvider(), cultures);
        var globalization = new GlobalizationService(cultures, translations);
        var events = new List<CultureChangedEventArgs>();
        globalization.CultureChanged += (_, args) => events.Add(args);

        globalization.SetRegionOverride(new RegionInfo("US"));
        globalization.SetRegionOverride(new RegionInfo("US"));
        globalization.SetMeasurementSystemOverride(MeasurementSystem.Metric);
        globalization.SetMeasurementSystemOverride(MeasurementSystem.Metric);
        globalization.ClearRegionOverride();
        globalization.ClearRegionOverride();
        globalization.ClearMeasurementSystemOverride();
        globalization.ClearMeasurementSystemOverride();

        Assert.Equal(4, events.Count);
        Assert.All(events, args =>
        {
            Assert.Equal("pl-PL", args.OldCulture.Name);
            Assert.Equal("pl-PL", args.OldUICulture.Name);
            Assert.Equal("pl-PL", args.NewCulture.Name);
            Assert.Equal("pl-PL", args.NewUICulture.Name);
        });
    }

    [Fact]
    public void StringLocalizerRegistrationReplacesDefaultProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IStringLocalizer<TestResources>>(new StaticStringLocalizer<TestResources>("Localized value"));

        services.AddProTranslate(culture: CultureInfo.GetCultureInfo("en-US"));
        services.AddProTranslateStringLocalizer<TestResources>();

        using ServiceProvider provider = services.BuildServiceProvider();
        var translations = provider.GetRequiredService<ITranslationService>();

        Assert.IsType<StringLocalizerTranslationProvider>(provider.GetRequiredService<ITranslationProvider>());
        Assert.Equal("Localized value", translations.GetString("Any.Key").Value);
    }

    [Fact]
    public void ResourceManagerProviderTreatsMissingManifestAsMissingResource()
    {
        var provider = new ResourceManagerTranslationProvider(
            new ResourceManager("ProTranslate.Tests.DoesNotExist", typeof(CoreTranslationTests).Assembly));

        LocalizedString value = provider.GetString("Missing.Key", CultureInfo.GetCultureInfo("en-US"));

        Assert.True(value.ResourceNotFound);
        Assert.Equal("Missing.Key", value.Value);
    }

    private sealed class ThrowingTranslationProvider : ITranslationProvider
    {
        public ThrowingTranslationProvider(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public LocalizedString GetString(string key, CultureInfo culture) =>
            throw new InvalidOperationException("Provider failed.");
    }

    private sealed class CountingTranslationProvider : ITranslationProvider
    {
        private readonly ITranslationProvider _inner;

        public CountingTranslationProvider(ITranslationProvider inner)
        {
            _inner = inner;
        }

        public int CallCount { get; private set; }

        public string Name => _inner.Name;

        public LocalizedString GetString(string key, CultureInfo culture)
        {
            CallCount++;
            return _inner.GetString(key, culture);
        }
    }

    private sealed class TestResources
    {
    }

    private sealed class StaticStringLocalizer<TResource> : IStringLocalizer<TResource>
    {
        private readonly string _value;

        public StaticStringLocalizer(string value)
        {
            _value = value;
        }

        public Microsoft.Extensions.Localization.LocalizedString this[string name] =>
            new(name, _value, resourceNotFound: false);

        public Microsoft.Extensions.Localization.LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(CultureInfo.CurrentCulture, _value, arguments), resourceNotFound: false);

        public IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<Microsoft.Extensions.Localization.LocalizedString>();
    }
}
