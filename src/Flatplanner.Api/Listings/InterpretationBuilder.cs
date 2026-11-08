using System.Globalization;
using Flatplanner.Api.Endpoints;
using Flatplanner.Core.Data;
using Flatplanner.Core.Search;

namespace Flatplanner.Api.Listings;

public static class InterpretationBuilder
{
    public const string Separator = " · ";

    public static Interpretation Build(SearchIntent intent, SearchMeta meta, IReadOnlyList<SearchWarning> warnings)
    {
        var chips = Chips(intent, meta).ToList();
        return new Interpretation(string.Join(Separator, chips.Select(c => c.Label)), chips, warnings);
    }

    private static IEnumerable<Chip> Chips(SearchIntent intent, SearchMeta meta)
    {
        if (intent.Cities.Length > 0)
        {
            var names = string.Join(", ", intent.Cities.Select(slug => EnumLabels.CityNames[slug]));
            yield return new Chip("cities", intent.RequestedCityOutOfScope is { } requested ? $"{names} (zamiast: {requested})" : names);
        }

        if (intent.Districts.Length > 0)
        {
            var names = intent.Districts.Select(key => meta.Districts.First(d => d.Key == key).Name);
            yield return new Chip("districts", string.Join(", ", names));
        }

        if (PriceLabel(intent) is { } price)
        {
            yield return new Chip("price", price);
        }

        if (AreaLabel(intent) is { } area)
        {
            yield return new Chip("area", area);
        }

        if (RoomsLabel(intent) is { } rooms)
        {
            yield return new Chip("rooms", rooms);
        }

        if (FloorLabel(intent) is { } floor)
        {
            yield return new Chip("floor", floor);
        }

        if (intent.Market is { } market)
        {
            yield return new Chip("market", EnumLabels.MarketOptions.First(o => o.Value == market).Label);
        }

        if (intent.AdvertiserType is { } advertiserType)
        {
            yield return new Chip("advertiserType", EnumLabels.AdvertiserTypeOptions.First(o => o.Value == advertiserType).Label);
        }

        if (RangeLabel(intent.BuildYearMin, intent.BuildYearMax, "rok budowy", "", grouped: false) is { } buildYear)
        {
            yield return new Chip("buildYear", buildYear);
        }

        if (RangeLabel(intent.PricePerM2Min, intent.PricePerM2Max, "", " zł/m²") is { } pricePerM2)
        {
            yield return new Chip("pricePerM2", pricePerM2);
        }

        if (RangeLabel(intent.RentMin, intent.RentMax, "czynsz", " zł") is { } rent)
        {
            yield return new Chip("rent", rent);
        }

        if (intent.FinishCondition is { } finishCondition)
        {
            yield return new Chip("finishCondition", Label(EnumLabels.FinishConditionOptions, finishCondition));
        }

        if (intent.Ownership is { } ownership)
        {
            yield return new Chip("ownership", Label(EnumLabels.OwnershipOptions, ownership));
        }

        if (intent.BuildingType is { } buildingType)
        {
            yield return new Chip("buildingType", Label(EnumLabels.BuildingTypeOptions, buildingType));
        }

        if (intent.Heating is { } heating)
        {
            yield return new Chip("heating", $"ogrzewanie: {Label(EnumLabels.HeatingOptions, heating)}");
        }

        if (intent.BuildingMaterial is { } buildingMaterial)
        {
            yield return new Chip("buildingMaterial", $"materiał: {Label(EnumLabels.BuildingMaterialOptions, buildingMaterial)}");
        }

        if (intent.RequireLift)
        {
            yield return new Chip("requireLift", "z windą");
        }

        if (intent.RequireBalcony)
        {
            yield return new Chip("requireBalcony", "z balkonem");
        }

        if (intent.RequireGarage)
        {
            yield return new Chip("requireGarage", "z garażem");
        }

        if (intent.RequireBasement)
        {
            yield return new Chip("requireBasement", "z piwnicą");
        }

        if (intent.Text is { } text)
        {
            yield return new Chip("text", $"„{text}”");
        }

        foreach (var preference in intent.Preferences)
        {
            yield return new Chip($"preferences:{preference}", PreferenceLabels[preference]);
        }

        foreach (var scope in intent.OutOfScope)
        {
            yield return new Chip($"outOfScope:{scope}", OutOfScopeLabels[scope]);
        }
    }

    private static readonly Dictionary<string, string> PreferenceLabels = new()
    {
        [Preference.Cheap] = "tanie: niższa cena za m² niż mediana miasta",
        [Preference.GoodCondition] = "ładne: stan „do zamieszkania”",
    };

    private static readonly Dictionary<string, string> OutOfScopeLabels = new()
    {
        [OutOfScope.Rent] = "wynajem: mamy tylko sprzedaż",
        [OutOfScope.House] = "dom: mamy tylko mieszkania",
        [OutOfScope.Metro] = "metro: w Trójmieście go nie ma",
    };

    private static string? PriceLabel(SearchIntent intent) => (intent.PriceMin, intent.PriceMax) switch
    {
        (null, null) => null,
        ({ } min, null) => $"od {Number(min)} zł",
        (null, { } max) => $"do {Number(max)} zł",
        ({ } min, { } max) => $"{Number(min)}–{Number(max)} zł",
    };

    private static string? AreaLabel(SearchIntent intent)
    {
        if (intent.AreaApprox is { } approx)
        {
            var (min, max) = SearchDefinitions.AreaApproxRange(approx);
            return $"ok. {Number(approx)} m² ({Number(min)}–{Number(max)} m²)";
        }

        return (intent.AreaMin, intent.AreaMax) switch
        {
            (null, null) => null,
            ({ } min, null) => $"od {Number(min)} m²",
            (null, { } max) => $"do {Number(max)} m²",
            ({ } min, { } max) => $"{Number(min)}–{Number(max)} m²",
        };
    }

    private static string? RoomsLabel(SearchIntent intent) => (intent.RoomsMin, intent.RoomsMax) switch
    {
        (null, null) => null,
        ({ } min, null) => $"od {min} {(min == 1 ? "pokoju" : "pokoi")}",
        (null, { } max) => $"do {max} {(max == 1 ? "pokoju" : "pokoi")}",
        ({ } min, { } max) when min == max => $"{min} {RoomsNominative(min)}",
        ({ } min, { } max) => $"{min}–{max} {RoomsNominative(max)}",
    };

    private static string RoomsNominative(int count) => count switch
    {
        1 => "pokój",
        >= 2 and <= 4 => "pokoje",
        _ => "pokoi",
    };

    private static string? FloorLabel(SearchIntent intent) => (intent.FloorMin, intent.FloorMax) switch
    {
        (null, null) => null,
        ({ } min, null) => $"piętro od {min}",
        (null, { } max) => $"piętro do {max}",
        ({ } min, { } max) when min == max => min switch { -1 => "suterena", 0 => "parter", _ => $"piętro {min}" },
        ({ } min, { } max) => $"piętro {min}–{max}",
    };

    private static string Label(IReadOnlyList<EnumOption> options, string value) => options.First(o => o.Value == value).Label;

    private static string? RangeLabel(int? min, int? max, string noun, string unit, bool grouped = true)
    {
        string Show(int value) => grouped ? Number(value) : value.ToString(CultureInfo.InvariantCulture);
        var prefix = noun.Length > 0 ? $"{noun} " : "";
        return (min, max) switch
        {
            (null, null) => null,
            ({ } from, null) => $"{prefix}od {Show(from)}{unit}",
            (null, { } to) => $"{prefix}do {Show(to)}{unit}",
            ({ } from, { } to) => $"{prefix}{Show(from)}–{Show(to)}{unit}",
        };
    }

    private static string Number(decimal value) =>
        value.ToString("#,0.##", CultureInfo.InvariantCulture).Replace(',', ' ').Replace('.', ',');
}
