namespace Flatplanner.Api.Chat;

public sealed record JudgedNeed(string Verdict, string Reason, IReadOnlyList<JudgedEvidence> Evidence);
