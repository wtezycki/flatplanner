namespace Flatplanner.Core.Search;

public sealed record AskIntent
{
    public string[] Cities { get; init; } = [];
    public string[] Districts { get; init; } = [];
    public int? PriceMax { get; init; }
    public decimal? AreaMin { get; init; }
    public decimal? AreaMax { get; init; }
    public int? RoomsMin { get; init; }
    public AskNeed[] Needs { get; init; } = [];
    public DeclinedNeed[] Declined { get; init; } = [];
    public string[] Clarifications { get; init; } = [];

    public SearchIntent ToSearchIntent() => new()
    {
        Cities = Cities,
        Districts = Districts,
        PriceMax = PriceMax,
        AreaMin = AreaMin,
        AreaMax = AreaMax,
        RoomsMin = RoomsMin,
    };
}
