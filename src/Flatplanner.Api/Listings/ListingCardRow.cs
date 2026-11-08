using System.Linq.Expressions;
using Flatplanner.Core.Data;

namespace Flatplanner.Api.Listings;

public sealed record ListingCardRow(
    long Id,
    string Title,
    int PricePln,
    decimal AreaM2,
    int PricePerM2,
    int Rooms,
    sbyte? Floor,
    byte? BuildingFloors,
    string CitySlug,
    string? DistrictName,
    string[] ImageUrls,
    string Market,
    DateTime CreatedAtSource,
    bool LocationApproximate,
    string[] QualityFlags,
    short? BuildYear,
    string? FinishCondition,
    string? Ownership,
    bool? HasLift,
    bool? HasBalcony,
    bool? HasGarage,
    bool? HasBasement,
    string? RisksJson,
    string? NearbyJson)
{
    public static readonly Expression<Func<Listing, ListingCardRow>> Projection = l => new ListingCardRow(
        l.Id, l.Title, l.PricePln, l.AreaM2, l.PricePerM2, l.Rooms, l.Floor, l.BuildingFloors, l.CitySlug, l.DistrictName,
        l.ImageUrls, l.Market, l.CreatedAtSource, l.LocationApproximate, l.QualityFlags, l.BuildYear, l.FinishCondition,
        l.Ownership, l.HasLift, l.HasBalcony, l.HasGarage, l.HasBasement, l.RisksJson, l.NearbyJson);

    private static readonly Func<Listing, ListingCardRow> Compiled = Projection.Compile();

    public static ListingCardRow From(Listing listing) => Compiled(listing);

    public ListingItem ToItem(IReadOnlyDictionary<string, int> medians)
    {
        var outlier = QualityFlags.Contains(QualityFlag.PriceOutlier);
        return new ListingItem(
            Id, Title, PricePln, AreaM2, PricePerM2, Rooms, Floor, QualityFlags.Contains(QualityFlag.FloorHigher10), BuildingFloors,
            CitySlug, Endpoints.EnumLabels.CityNames[CitySlug], DistrictName, ImageUrls, Market,
            DateTime.SpecifyKind(CreatedAtSource, DateTimeKind.Utc), LocationApproximate, outlier,
            BuildYear, FinishCondition, Ownership, HasLift, HasBalcony, HasGarage, HasBasement,
            CardSignals.RiskKinds(RisksJson), CardSignals.NearestStop(NearbyJson, LocationApproximate),
            CardSignals.NearestPark(NearbyJson, LocationApproximate),
            outlier ? null : CardSignals.BelowMedianPercent(PricePerM2, medians.GetValueOrDefault(CitySlug)));
    }
}
