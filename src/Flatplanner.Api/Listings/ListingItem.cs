namespace Flatplanner.Api.Listings;

public sealed record ListingItem(
    long Id,
    string Title,
    int PricePln,
    decimal AreaM2,
    int PricePerM2,
    int Rooms,
    int? Floor,
    bool FloorHigherThan10,
    int? BuildingFloors,
    string CitySlug,
    string CityName,
    string? DistrictName,
    IReadOnlyList<string> ImageUrls,
    string Market,
    DateTime CreatedAtSource,
    bool LocationApproximate,
    bool PriceOutlier);
