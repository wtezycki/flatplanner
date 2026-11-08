namespace Flatplanner.Ingest.Evaluation;

public sealed record ExpectedIntent
{
    public string[] Cities { get; init; } = [];
    public string[] Districts { get; init; } = [];
    public int? PriceMax { get; init; }
    public decimal? AreaMin { get; init; }
    public decimal? AreaMax { get; init; }
    public int? RoomsMin { get; init; }
    public int NeedsMin { get; init; }
    public int DeclinedMin { get; init; }
    public int ClarificationsMin { get; init; }
}
