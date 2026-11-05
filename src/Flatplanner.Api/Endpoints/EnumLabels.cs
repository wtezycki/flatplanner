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
