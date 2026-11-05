namespace Flatplanner.Api.Endpoints;

public sealed record MetaResponse(
    IReadOnlyList<CityMeta> Cities,
    string DefaultCitySlug,
    MetaEnums Enums,
    string ChatMode,
    int ChatMaxLength,
    DateTime? DataAsOf);
