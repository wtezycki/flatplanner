namespace Flatplanner.Ingest.Evaluation;

public sealed record AskAnswerItem(AskAnswerListing Listing, string Group, IReadOnlyList<NeedVerdict> Needs);
