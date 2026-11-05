namespace Flatplanner.Api.Endpoints;

public sealed record CityMeta(string Slug, string Name, int Count, IReadOnlyList<DistrictMeta> Districts);
