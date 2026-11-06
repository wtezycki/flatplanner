namespace Flatplanner.Api.Chat;

public sealed record NeedResult(string Verdict, string? Reason, IReadOnlyList<Evidence> Evidence)
{
    public static readonly NeedResult Unknown = new(Chat.Verdict.Unknown, null, []);
}
