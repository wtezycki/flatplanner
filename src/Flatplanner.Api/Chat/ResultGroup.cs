namespace Flatplanner.Api.Chat;

public static class ResultGroup
{
    public const string Full = "full";
    public const string Partial = "partial";
    public const string Near = "near";
    public const string Unknown = "unknown";

    public static readonly string[] Ordered = [Full, Partial, Near, Unknown];
}
