namespace Flatplanner.Api.Chat;

public static class EvidenceKind
{
    public const string Quote = "quote";
    public const string Place = "place";
    public const string Field = "field";

    public static readonly string[] All = [Quote, Place, Field];
}
