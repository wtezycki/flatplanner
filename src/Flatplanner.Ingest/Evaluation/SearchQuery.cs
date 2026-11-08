namespace Flatplanner.Ingest.Evaluation;

public sealed record SearchQuery(string Id, string Message, bool FallbackExpected, ExpectedIntent Expected)
{
    public string[] History { get; init; } = [];
}
