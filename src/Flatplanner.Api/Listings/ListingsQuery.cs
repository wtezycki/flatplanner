using Flatplanner.Core.Search;

namespace Flatplanner.Api.Listings;

public sealed record ListingsQuery(
    string[]? Cities,
    string[]? Districts,
    int? PriceMin,
    int? PriceMax,
    decimal? AreaMin,
    decimal? AreaMax,
    decimal? AreaApprox,
    int? RoomsMin,
    int? RoomsMax,
    int? FloorMin,
    int? FloorMax,
    string? Market,
    string? AdvertiserType,
    bool? RequireLift,
    bool? RequireBalcony,
    string? Text,
    string[]? Preferences,
    string? RequestedCityOutOfScope,
    string[]? OutOfScope,
    string? Sort,
    int? Page)
{
    public SearchIntent ToIntent() => new()
    {
        Cities = Cities ?? [],
        Districts = Districts ?? [],
        PriceMin = PriceMin,
        PriceMax = PriceMax,
        AreaMin = AreaMin,
        AreaMax = AreaMax,
        AreaApprox = AreaApprox,
        RoomsMin = RoomsMin,
        RoomsMax = RoomsMax,
        FloorMin = FloorMin,
        FloorMax = FloorMax,
        Market = Market,
        AdvertiserType = AdvertiserType,
        RequireLift = RequireLift ?? false,
        RequireBalcony = RequireBalcony ?? false,
        Text = Text,
        Preferences = Preferences ?? [],
        RequestedCityOutOfScope = RequestedCityOutOfScope,
        OutOfScope = OutOfScope ?? [],
    };
}
