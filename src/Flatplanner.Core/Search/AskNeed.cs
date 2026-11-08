namespace Flatplanner.Core.Search;

public sealed record AskNeed(string Text, NeedPlace[] Places, string[] Keywords)
{
    public bool Mentions(string foldedText) => Keywords.Any(k => foldedText.Contains(k, StringComparison.Ordinal));
}
