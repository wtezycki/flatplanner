using System.Globalization;
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

        var (priceMin, priceMax) = ClipRange("Cena", "priceMin", "priceMax", intent.PriceMin, intent.PriceMax, SearchDefinitions.PriceMin, SearchDefinitions.PriceMax, warnings);
        var (areaMin, areaMax) = ClipRange("Metraż", "areaMin", "areaMax", intent.AreaMin, intent.AreaMax, SearchDefinitions.AreaMin, SearchDefinitions.AreaMax, warnings);
        var (roomsMin, roomsMax) = ClipRange("Liczba pokoi", "roomsMin", "roomsMax", intent.RoomsMin, intent.RoomsMax, SearchDefinitions.RoomsMin, SearchDefinitions.RoomsMax, warnings);
        var (floorMin, floorMax) = ClipRange("Piętro", "floorMin", "floorMax", intent.FloorMin, intent.FloorMax, SearchDefinitions.FloorMin, SearchDefinitions.FloorMax, warnings);

        var (areaApprox, _) = ClipRange("Przybliżony metraż", "areaApprox", "areaApprox", intent.AreaApprox, null, SearchDefinitions.AreaMin, SearchDefinitions.AreaMax, warnings);
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
            Market = Market.All.Contains(intent.Market) ? intent.Market : null,
            AdvertiserType = AdvertiserType.All.Contains(intent.AdvertiserType) ? intent.AdvertiserType : null,
            Text = string.IsNullOrEmpty(text) ? null : text,
            Preferences = Preference.All.Where(intent.Preferences.Contains).ToArray(),
            RequestedCityOutOfScope = requestedCity,
            OutOfScope = Data.OutOfScope.All.Where(intent.OutOfScope.Contains).ToArray(),
        };

        return new NormalizedSearch(normalized, warnings);
    }

    private static (T? Min, T? Max) ClipRange<T>(
        string label,
        string minField,
        string maxField,
        T? min,
        T? max,
        T lower,
        T upper,
        List<SearchWarning> warnings)
        where T : struct, INumber<T>
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }

        return (Clip(minField, min), Clip(maxField, max));

        T? Clip(string field, T? value)
        {
            if (value is not { } v || (v >= lower && v <= upper))
            {
                return value;
            }

            warnings.Add(new SearchWarning(field, $"{label} {Show(v)} pominięte: poza zakresem {Show(lower)}–{Show(upper)}."));
            return null;
        }
    }

    private static string Show<T>(T value) where T : INumber<T> =>
        value.ToString(null, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^\p{L}[\p{L} \-]{1,39}$")]
    private static partial Regex RequestedCityPattern();
}
