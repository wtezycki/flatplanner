namespace Flatplanner.Core.Search;

public sealed record SearchMeta(
    IReadOnlyList<string> CitySlugs,
    IReadOnlyList<SearchMetaDistrict> Districts,
    string DefaultCitySlug);
