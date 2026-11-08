namespace Flatplanner.Ingest.Evaluation;

public sealed record JudgeResult(
    string CaseId,
    long Listing,
    string[] ExpectedVerdicts,
    string? Need,
    NeedVerdict? Actual,
    string? Difference);
