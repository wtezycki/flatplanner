namespace Flatplanner.Api.Chat;

public static class Verdict
{
    public const string Yes = "yes";
    public const string Partly = "partly";
    public const string No = "no";
    public const string Unknown = "unknown";

    public static readonly string[] All = [Yes, Partly, No, Unknown];
}
