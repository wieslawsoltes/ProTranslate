using System.Collections.Concurrent;
using System.Globalization;

namespace ProTranslate;

/// <summary>
/// Provides translations from in-memory dictionaries.
/// </summary>
public sealed class InMemoryTranslationProvider : ITranslationProvider
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _resources = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTranslationProvider"/> class.
    /// </summary>
    public InMemoryTranslationProvider()
        : this("InMemory")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTranslationProvider"/> class.
    /// </summary>
    /// <param name="name">The provider name.</param>
    public InMemoryTranslationProvider(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// Adds or replaces one localized value.
    /// </summary>
    /// <param name="culture">The culture for the value.</param>
    /// <param name="key">The resource key.</param>
    /// <param name="value">The localized value.</param>
    /// <returns>The current provider.</returns>
    public InMemoryTranslationProvider Add(CultureInfo culture, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        ConcurrentDictionary<string, string> cultureResources = _resources.GetOrAdd(
            NormalizeCultureName(culture),
            static _ => new ConcurrentDictionary<string, string>(StringComparer.Ordinal));

        cultureResources[key] = value;
        return this;
    }

    /// <summary>
    /// Adds or replaces localized values for a culture.
    /// </summary>
    /// <param name="culture">The culture for the values.</param>
    /// <param name="values">The localized values.</param>
    /// <returns>The current provider.</returns>
    public InMemoryTranslationProvider Add(CultureInfo culture, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (KeyValuePair<string, string> value in values)
        {
            Add(culture, value.Key, value.Value);
        }

        return this;
    }

    /// <inheritdoc />
    public LocalizedString GetString(string key, CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(culture);

        if (_resources.TryGetValue(NormalizeCultureName(culture), out ConcurrentDictionary<string, string>? values)
            && values.TryGetValue(key, out string? value))
        {
            return new LocalizedString(key, value, culture, false, Name);
        }

        return new LocalizedString(key, key, culture, true, Name);
    }

    private static string NormalizeCultureName(CultureInfo culture) => culture.Name;
}
