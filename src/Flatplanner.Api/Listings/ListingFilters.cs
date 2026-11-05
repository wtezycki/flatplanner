using Flatplanner.Core.Data;
using Flatplanner.Core.Search;
using Flatplanner.Core.Text;
using Microsoft.EntityFrameworkCore;

namespace Flatplanner.Api.Listings;

public static class ListingFilters
{
    public static IQueryable<Listing> Apply(this IQueryable<Listing> source, SearchIntent intent)
    {
        var query = source;

        if (intent.Cities.Length > 0)
        {
            query = query.Where(l => intent.Cities.Contains(l.CitySlug));
        }

        if (intent.Districts.Length > 0)
        {
            query = query.Where(l => intent.Districts.Contains(l.CitySlug + ":" + l.DistrictSlug));
        }

        if (intent.PriceMin is { } priceMin)
        {
            query = query.Where(l => l.PricePln >= priceMin);
        }

        if (intent.PriceMax is { } priceMax)
        {
            query = query.Where(l => l.PricePln <= priceMax);
        }

        var (areaMin, areaMax) = AreaRange(intent);
        if (areaMin is { } min)
        {
            query = query.Where(l => l.AreaM2 >= min);
        }

        if (areaMax is { } max)
        {
            query = query.Where(l => l.AreaM2 <= max);
        }

        if (intent.RoomsMin is { } roomsMin)
        {
            query = query.Where(l => l.Rooms >= roomsMin);
        }

        if (intent.RoomsMax is { } roomsMax)
        {
            query = query.Where(l => l.Rooms <= roomsMax);
        }

        if (intent.FloorMin is { } floorMin)
        {
            query = query.Where(l => l.Floor >= floorMin);
        }

        if (intent.FloorMax is { } floorMax)
        {
            query = query.Where(l => l.Floor <= floorMax);
        }

        if (intent.Market is { } market)
        {
            query = query.Where(l => l.Market == market);
        }

        if (intent.AdvertiserType is { } advertiserType)
        {
            query = query.Where(l => l.AdvertiserType == advertiserType);
        }

        if (intent.RequireLift)
        {
            query = query.Where(l => l.HasLift == true);
        }

        if (intent.RequireBalcony)
        {
            query = query.Where(l => l.HasBalcony == true);
        }

        return intent.Text is null ? query : query.ApplyText(intent.Text);
    }

    private static (decimal? Min, decimal? Max) AreaRange(SearchIntent intent) =>
        intent.AreaApprox is { } approx
            ? SearchDefinitions.AreaApproxRange(approx)
            : (intent.AreaMin, intent.AreaMax);

    private static IQueryable<Listing> ApplyText(this IQueryable<Listing> query, string text)
    {
        var tokens = TextFolding.Fold(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= SearchDefinitions.TextMinTokenLength)
            .Take(SearchDefinitions.TextMaxTokens);

        foreach (var token in tokens)
        {
            var pattern = $"%{token}%";
            query = query.Where(l => EF.Functions.Like(l.SearchText, pattern, "\\"));
        }

        return query;
    }
}
