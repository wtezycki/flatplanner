namespace Flatplanner.Ingest.Evaluation;

public sealed record AskAnswer(
    IReadOnlyList<string> Needs,
    IReadOnlyList<AskAnswerItem> Items,
    int RemainingCount,
    bool UsedFallback,
    string? FallbackReason);
