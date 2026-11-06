using Flatplanner.Core.Data;

namespace Flatplanner.Api.Chat;

public sealed record AskItem(
    long Id,
    string Title,
    int PricePln,
    decimal AreaM2,
    int Rooms,
    int? Floor,
    int? BuildingFloors,
    string CityName,
    string? DistrictName,
    string? ImageUrl,
    string? AiSummary,
    bool LocationApproximate,
    string Group,
    IReadOnlyList<NeedResult> Needs,
    IReadOnlyList<ListingRisk> Risks,
    int? TotalWithExtrasPln);
