using System.Numerics;
using System.Text.RegularExpressions;
using Flatplanner.Core.Data;

namespace Flatplanner.Core.Search;

public static partial class SearchIntentNormalizer
{
    public static NormalizedSearch Normalize(SearchIntent intent, SearchMeta meta)
    {
        var warnings = new List<SearchWarning>();

        var cities = City.All
            .Where(slug => meta.CitySlugs.Contains(slug) && intent.Cities.Contains(slug))
            .ToArray();

        var knownDistricts = meta.Districts.ToDictionary(d => d.Key);
        var requestedDistricts = intent.Districts.Distinct().ToArray();
        var districts = requestedDistricts.Where(knownDistricts.ContainsKey).ToArray();
        if (districts.Length < requestedDistricts.Length)
        {
            warnings.Add(new SearchWarning("districts", "Nieznana dzielnica pominięta."));
        }

        if (cities.Length > 0)
        {
            districts = districts.Where(d => cities.Contains(knownDistricts[d].CitySlug)).ToArray();
        }
        else if (districts.Length > 0)
        {
            cities = City.All.Where(slug => districts.Any(d => knownDistricts[d].CitySlug == slug)).ToArray();
        }

        var requestedCity = intent.RequestedCityOutOfScope?.Trim();
        if (requestedCity is null || !RequestedCityPattern().IsMatch(requestedCity))
        {
            requestedCity = null;
        }

        if (requestedCity is not null && cities.Length == 0)
        {
            cities = [meta.DefaultCitySlug];
        }

        var (priceMin, priceMax) = Ordered(intent.PriceMin, intent.PriceMax);
        var (areaMin, areaMax) = Ordered(intent.AreaMin, intent.AreaMax);
        var (roomsMin, roomsMax) = Ordered(intent.RoomsMin, intent.RoomsMax);
        var (floorMin, floorMax) = Ordered(intent.FloorMin, intent.FloorMax);

        var (buildYearMin, buildYearMax) = Ordered(intent.BuildYearMin, intent.BuildYearMax);
        var (pricePerM2Min, pricePerM2Max) = Ordered(intent.PricePerM2Min, intent.PricePerM2Max);
        var (rentMin, rentMax) = Ordered(intent.RentMin, intent.RentMax);

        var areaApprox = intent.AreaApprox is > 0 ? intent.AreaApprox : null;
        if (areaApprox is not null && (areaMin is not null || areaMax is not null))
        {
            areaApprox = null;
            warnings.Add(new SearchWarning("areaApprox", "Przybliżony metraż zastąpiony zakresem."));
        }

        var text = intent.Text?.Trim();
        if (text is { Length: > SearchDefinitions.TextMaxLength })
        {
            text = text[..SearchDefinitions.TextMaxLength];
        }

        var normalized = intent with
        {
            Cities = cities,
            Districts = districts,
            PriceMin = priceMin,
            PriceMax = priceMax,
            AreaMin = areaMin,
            AreaMax = areaMax,
            AreaApprox = areaApprox,
            RoomsMin = roomsMin,
            RoomsMax = roomsMax,
            FloorMin = floorMin,
            FloorMax = floorMax,
            BuildYearMin = buildYearMin,
            BuildYearMax = buildYearMax,
            PricePerM2Min = pricePerM2Min,
            PricePerM2Max = pricePerM2Max,
            RentMin = rentMin,
            RentMax = rentMax,
            Market = Market.All.Contains(intent.Market) ? intent.Market : null,
            AdvertiserType = AdvertiserType.All.Contains(intent.AdvertiserType) ? intent.AdvertiserType : null,
            FinishCondition = FinishCondition.All.Contains(intent.FinishCondition) ? intent.FinishCondition : null,
            Ownership = Ownership.All.Contains(intent.Ownership) ? intent.Ownership : null,
            BuildingType = BuildingType.All.Contains(intent.BuildingType) ? intent.BuildingType : null,
            Heating = Heating.All.Contains(intent.Heating) ? intent.Heating : null,
            BuildingMaterial = BuildingMaterial.All.Contains(intent.BuildingMaterial) ? intent.BuildingMaterial : null,
            Text = string.IsNullOrEmpty(text) ? null : text,
            Preferences = Preference.All.Where(intent.Preferences.Contains).ToArray(),
            RequestedCityOutOfScope = requestedCity,
            OutOfScope = Data.OutOfScope.All.Where(intent.OutOfScope.Contains).ToArray(),
        };

        return new NormalizedSearch(normalized, warnings);
    }

    private static (T? Min, T? Max) Ordered<T>(T? min, T? max)
        where T : struct, INumber<T> =>
        min > max ? (max, min) : (min, max);

    [GeneratedRegex(@"^\p{L}[\p{L} \-]{1,39}$")]
    private static partial Regex RequestedCityPattern();
}
