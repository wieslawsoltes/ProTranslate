using System.Globalization;
using System.Resources;

namespace ProTranslate;

/// <summary>
/// Provides translations through <see cref="System.Resources.ResourceManager"/>.
/// </summary>
public sealed class ResourceManagerTranslationProvider : ITranslationProvider
{
    private readonly ResourceManager _resourceManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManagerTranslationProvider"/> class.
    /// </summary>
    /// <param name="resourceManager">The resource manager.</param>
    public ResourceManagerTranslationProvider(ResourceManager resourceManager)
        : this(resourceManager, "ResourceManager")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManagerTranslationProvider"/> class.
    /// </summary>
    /// <param name="resourceManager">The resource manager.</param>
    /// <param name="name">The provider name.</param>
    public ResourceManagerTranslationProvider(ResourceManager resourceManager, string name)
    {
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public LocalizedString GetString(string key, CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(culture);

        try
        {
            string? value = _resourceManager.GetString(key, culture);
            if (value is not null)
            {
                return new LocalizedString(key, value, culture, false, Name);
            }
        }
        catch (MissingManifestResourceException)
        {
        }
        catch (MissingSatelliteAssemblyException)
        {
        }

        return new LocalizedString(key, key, culture, true, Name);
    }
}
