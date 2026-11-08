namespace Flatplanner.Core.Search;

public sealed record AskNeed(string Text, NeedPlace[] Places, string[] Keywords);
