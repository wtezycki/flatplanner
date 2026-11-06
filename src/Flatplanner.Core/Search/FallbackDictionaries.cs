using Flatplanner.Core.Data;

namespace Flatplanner.Core.Search;

public static class FallbackDictionaries
{
    public static readonly IReadOnlyDictionary<string, string> CityPatterns = new Dictionary<string, string>
    {
        [City.Gdansk] = "gdansk|gdanska|gdansku",
        [City.Gdynia] = "gdynia|gdyni|gdynii|gdynie",
        [City.Sopot] = "sopot|sopotu|sopocie",
    };

    public const string TricityPattern = "trojmiasto|trojmiescie|trojmiasta|3miasto";
}
