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
    public bool RequireLift { get; init; }
    public bool RequireBalcony { get; init; }
    public string? Text { get; init; }
    public string[] Preferences { get; init; } = [];
    public string? RequestedCityOutOfScope { get; init; }
    public string[] OutOfScope { get; init; } = [];
}
