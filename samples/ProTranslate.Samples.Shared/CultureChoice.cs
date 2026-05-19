namespace ProTranslate.Samples.Shared;

public sealed record CultureChoice(string CultureName, string DefaultRegionName, string DisplayName);

public sealed record RegionChoice(string? RegionName, string DisplayName);

