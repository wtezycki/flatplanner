using Flatplanner.Core.Search;
using Microsoft.AspNetCore.Mvc;

namespace Flatplanner.Api.Listings;

public sealed record ListingsQuery(
    [FromQuery] string[]? Cities,
    [FromQuery] string[]? Districts,
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
    [FromQuery] string[]? Preferences,
    string? RequestedCityOutOfScope,
    [FromQuery] string[]? OutOfScope,
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
