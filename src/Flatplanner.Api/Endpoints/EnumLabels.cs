using Flatplanner.Core.Data;

namespace Flatplanner.Api.Endpoints;

internal static class EnumLabels
{
    public static readonly IReadOnlyDictionary<string, string> CityNames = new Dictionary<string, string>
    {
        [City.Gdansk] = "Gdańsk",
        [City.Gdynia] = "Gdynia",
        [City.Sopot] = "Sopot",
    };

    public static readonly IReadOnlyList<EnumOption> MarketOptions =
    [
        new(Market.Primary, "rynek pierwotny"),
        new(Market.Secondary, "rynek wtórny"),
    ];

    public static readonly IReadOnlyList<EnumOption> AdvertiserTypeOptions =
    [
        new(AdvertiserType.Agency, "biuro nieruchomości"),
        new(AdvertiserType.Private, "osoba prywatna"),
    ];

    public static readonly IReadOnlyList<EnumOption> FinishConditionOptions =
    [
        new(FinishCondition.ReadyToUse, "do zamieszkania"),
        new(FinishCondition.ToCompletion, "do wykończenia"),
        new(FinishCondition.ToRenovation, "do remontu"),
    ];

    public static readonly IReadOnlyList<EnumOption> OwnershipOptions =
    [
        new(Ownership.Full, "pełna własność"),
        new(Ownership.Limited, "spółdzielcze własnościowe prawo do lokalu"),
    ];

    public static readonly IReadOnlyList<EnumOption> HeatingOptions =
    [
        new(Heating.Urban, "miejskie"),
        new(Heating.Gas, "gazowe"),
        new(Heating.BoilerRoom, "kotłownia"),
        new(Heating.Electrical, "elektryczne"),
    ];

    public static readonly IReadOnlyList<EnumOption> BuildingTypeOptions =
    [
        new(BuildingType.Block, "blok"),
        new(BuildingType.Apartment, "apartamentowiec"),
        new(BuildingType.Tenement, "kamienica"),
    ];

    public static readonly IReadOnlyList<EnumOption> WindowsTypeOptions =
    [
        new(WindowsType.Plastic, "plastikowe"),
        new(WindowsType.Wooden, "drewniane"),
        new(WindowsType.Aluminium, "aluminiowe"),
    ];

    public static readonly IReadOnlyList<EnumOption> BuildingMaterialOptions =
    [
        new(BuildingMaterial.Brick, "cegła"),
        new(BuildingMaterial.Silikat, "silikat"),
        new(BuildingMaterial.Breezeblock, "pustak"),
        new(BuildingMaterial.ConcretePlate, "wielka płyta"),
        new(BuildingMaterial.Concrete, "beton"),
        new(BuildingMaterial.CellularConcrete, "beton komórkowy"),
        new(BuildingMaterial.ReinforcedConcrete, "żelbet"),
        new(BuildingMaterial.Other, "inny"),
    ];

    public static readonly IReadOnlyList<EnumOption> FactStatusOptions =
    [
        new(FactStatus.Included, "w cenie"),
        new(FactStatus.ExtraCost, "dodatkowo płatne"),
        new(FactStatus.NotAvailable, "brak"),
    ];

    public static readonly IReadOnlyList<EnumOption> SortOptions =
    [
        new(SortOrder.Newest, "najnowsze"),
        new(SortOrder.PriceAsc, "najtańsze"),
        new(SortOrder.PriceDesc, "najdroższe"),
        new(SortOrder.PricePerM2Asc, "najniższa cena za m²"),
        new(SortOrder.Relevance, "najlepiej dopasowane"),
    ];

    public static readonly IReadOnlyList<EnumOption> ProvenanceOptions =
    [
        new(Provenance.Listing, "z ogłoszenia"),
        new(Provenance.Description, "z opisu"),
        new(Provenance.Computed, "wyliczone"),
        new(Provenance.Missing, "brak danych"),
    ];
}
