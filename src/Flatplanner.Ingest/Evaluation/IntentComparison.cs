using Flatplanner.Core.Search;

namespace Flatplanner.Ingest.Evaluation;

public static class IntentComparison
{
    // Hard fields are compared after the normalizer, as the ask pipeline sees them; the model-written parts only by count (design 12).
    public static IReadOnlyList<string> Differences(ExpectedIntent expected, AskIntent actual, SearchMeta meta, bool checkModelParts)
    {
        var intent = SearchIntentNormalizer.Normalize(actual.ToSearchIntent(), meta).Intent;
        var differences = new List<string>();

        Set("cities", expected.Cities, intent.Cities);
        Set("districts", expected.Districts, intent.Districts);
        Value("priceMax", expected.PriceMax, intent.PriceMax);
        Value("areaMin", expected.AreaMin, intent.AreaMin);
        Value("areaMax", expected.AreaMax, intent.AreaMax);
        Value("roomsMin", expected.RoomsMin, intent.RoomsMin);

        if (checkModelParts)
        {
            AtLeast("needs", expected.NeedsMin, actual.Needs.Length);
            AtLeast("declined", expected.DeclinedMin, actual.Declined.Length);
            AtLeast("clarifications", expected.ClarificationsMin, actual.Clarifications.Length);
        }

        return differences;

        void Set(string name, string[] want, string[] got)
        {
            if (!want.ToHashSet().SetEquals(got))
            {
                differences.Add($"{name}: expected [{string.Join(", ", want)}], got [{string.Join(", ", got)}]");
            }
        }

        void Value<T>(string name, T? want, T? got)
            where T : struct
        {
            if (!Nullable.Equals(want, got))
            {
                differences.Add($"{name}: expected {want?.ToString() ?? "null"}, got {got?.ToString() ?? "null"}");
            }
        }

        void AtLeast(string name, int want, int got)
        {
            if (got < want)
            {
                differences.Add($"{name}: expected at least {want}, got {got}");
            }
        }
    }
}
