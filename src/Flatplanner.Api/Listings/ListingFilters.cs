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

        if (intent.BuildYearMin is { } buildYearMin)
        {
            query = query.Where(l => l.BuildYear >= buildYearMin);
        }

        if (intent.BuildYearMax is { } buildYearMax)
        {
            query = query.Where(l => l.BuildYear <= buildYearMax);
        }

        if (intent.PricePerM2Min is { } pricePerM2Min)
        {
            query = query.Where(l => l.PricePerM2 >= pricePerM2Min);
        }

        if (intent.PricePerM2Max is { } pricePerM2Max)
        {
            query = query.Where(l => l.PricePerM2 <= pricePerM2Max);
        }

        if (intent.RentMin is { } rentMin)
        {
            query = query.Where(l => l.RentPln >= rentMin);
        }

        if (intent.RentMax is { } rentMax)
        {
            query = query.Where(l => l.RentPln <= rentMax);
        }

        if (intent.FinishConditions.Length > 0)
        {
            query = query.Where(l => l.FinishCondition != null && intent.FinishConditions.Contains(l.FinishCondition));
        }

        if (intent.Ownerships.Length > 0)
        {
            query = query.Where(l => l.Ownership != null && intent.Ownerships.Contains(l.Ownership));
        }

        if (intent.BuildingTypes.Length > 0)
        {
            query = query.Where(l => l.BuildingType != null && intent.BuildingTypes.Contains(l.BuildingType));
        }

        if (intent.Heatings.Length > 0)
        {
            query = query.Where(l => l.Heating != null && intent.Heatings.Contains(l.Heating));
        }

        if (intent.BuildingMaterials.Length > 0)
        {
            query = query.Where(l => l.BuildingMaterial != null && intent.BuildingMaterials.Contains(l.BuildingMaterial));
        }

        if (intent.RequireLift)
        {
            query = query.Where(l => l.HasLift == true);
        }

        if (intent.RequireBalcony)
        {
            query = query.Where(l => l.HasBalcony == true);
        }

        if (intent.RequireGarage)
        {
            query = query.Where(l => l.HasGarage == true);
        }

        if (intent.RequireBasement)
        {
            query = query.Where(l => l.HasBasement == true);
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
