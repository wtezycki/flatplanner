namespace Flatplanner.Core.Search;

public sealed record SearchIntent
{
    public string[] Cities { get; init; } = [];
    public string[] Districts { get; init; } = [];
    public int? PriceMin { get; init; }
    public int? PriceMax { get; init; }
    public decimal? AreaMin { get; init; }
    public decimal? AreaMax { get; init; }
    public decimal? AreaApprox { get; init; }
    public int? RoomsMin { get; init; }
    public int? RoomsMax { get; init; }
    public int? FloorMin { get; init; }
    public int? FloorMax { get; init; }
    public string? Market { get; init; }
    public string? AdvertiserType { get; init; }
    public int? BuildYearMin { get; init; }
    public int? BuildYearMax { get; init; }
    public int? PricePerM2Min { get; init; }
    public int? PricePerM2Max { get; init; }
    public int? RentMin { get; init; }
    public int? RentMax { get; init; }
    public string? FinishCondition { get; init; }
    public string? Ownership { get; init; }
    public string? BuildingType { get; init; }
    public string? Heating { get; init; }
    public string? BuildingMaterial { get; init; }
    public bool RequireLift { get; init; }
    public bool RequireBalcony { get; init; }
    public bool RequireGarage { get; init; }
    public bool RequireBasement { get; init; }
    public string? Text { get; init; }
    public string[] Preferences { get; init; } = [];
    public string? RequestedCityOutOfScope { get; init; }
    public string[] OutOfScope { get; init; } = [];
}
