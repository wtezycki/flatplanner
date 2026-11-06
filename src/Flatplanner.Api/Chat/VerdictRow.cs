namespace Flatplanner.Api.Chat;

public sealed record VerdictRow(long Id, DateTime CreatedAtSource, IReadOnlyList<string> Verdicts);
