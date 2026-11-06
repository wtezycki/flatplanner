using System.Globalization;
using Flatplanner.Api.Endpoints;
using Flatplanner.Core.Data;

namespace Flatplanner.Api.Chat;

public static class ListingFields
{
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    public static IReadOnlyList<ListingField> Of(Listing listing)
    {
        var floor = FloorLabel(listing);
        ListingField?[] fields =
        [
            new("areaM2", listing.AreaM2, $"Powierzchnia: {listing.AreaM2.ToString("0.##", Polish)} m²"),
            new("rooms", (int)listing.Rooms, $"Pokoje: {listing.Rooms}"),
            Field("floor", listing.Floor, floor),
            Field("buildingFloors", listing.BuildingFloors, floor),
            Field("buildYear", listing.BuildYear, $"Rok budowy: {listing.BuildYear}"),
            Option("market", "Rynek", listing.Market, EnumLabels.MarketOptions),
            Option("finishCondition", "Stan wykończenia", listing.FinishCondition, EnumLabels.FinishConditionOptions),
            Option("buildingType", "Typ budynku", listing.BuildingType, EnumLabels.BuildingTypeOptions),
            Option("buildingMaterial", "Materiał budynku", listing.BuildingMaterial, EnumLabels.BuildingMaterialOptions),
            Option("windowsType", "Okna", listing.WindowsType, EnumLabels.WindowsTypeOptions),
            Option("heating", "Ogrzewanie", listing.Heating, EnumLabels.HeatingOptions),
            Option("ownership", "Forma własności", listing.Ownership, EnumLabels.OwnershipOptions),
            Field("rentPln", listing.RentPln, $"Czynsz (zł/mies.): {listing.RentPln}"),
            Flag("hasLift", "Winda", listing.HasLift),
            Flag("hasBalcony", "Balkon", listing.HasBalcony),
            Flag("hasGarage", "Garaż / miejsce postojowe", listing.HasGarage),
            Flag("hasBasement", "Piwnica", listing.HasBasement),
            Option("advertiserType", "Ogłaszający", listing.AdvertiserType, EnumLabels.AdvertiserTypeOptions),
            Field("districtName", listing.DistrictName, $"Dzielnica: {listing.DistrictName}"),
            Field("street", listing.Street, $"Ulica: {listing.Street}"),
        ];

        return fields.OfType<ListingField>().ToList();
    }

    private static ListingField? Field(string name, object? value, string? label) =>
        value is null || label is null ? null : new ListingField(name, value, label);

    private static ListingField? Option(string name, string label, string? value, IReadOnlyList<EnumOption> options) =>
        options.FirstOrDefault(o => o.Value == value) is { } option ? new ListingField(name, value!, $"{label}: {option.Label}") : null;

    private static ListingField? Flag(string name, string label, bool? value) =>
        value is { } flag ? new ListingField(name, flag, $"{label}: {(flag ? "tak" : "nie")}") : null;

    private static string? FloorLabel(Listing listing)
    {
        if (listing.Floor is not { } floor)
        {
            return null;
        }

        var value = floor switch
        {
            < 0 => "suterena",
            0 => "parter",
            _ => listing.QualityFlags.Contains(QualityFlag.FloorHigher10) ? $"{floor}+" : $"{floor}",
        };

        return floor > 0 && listing.BuildingFloors is { } total ? $"Piętro: {value} z {total}" : $"Piętro: {value}";
    }
}
