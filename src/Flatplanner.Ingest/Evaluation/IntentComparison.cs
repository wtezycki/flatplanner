using Flatplanner.Core.Search;
using Flatplanner.Core.Text;

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
            if (expected.NeedsMax is { } needsMax && actual.Needs.Length > needsMax)
            {
                differences.Add($"needs: expected at most {needsMax}, got {actual.Needs.Length}");
            }

            AtLeast("declined", expected.DeclinedMin, actual.Declined.Length);
            AtLeast("clarifications", expected.ClarificationsMin, actual.Clarifications.Length);
            MissingPlaces(expected.NeedPlaces, actual.Needs);
            MissingNeedTexts(expected.NeedTextContains, actual.Needs);
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

        // "tag" or "tag|extra"; one place among all needs is enough.
        void MissingPlaces(string[] want, AskNeed[] needs)
        {
            var places = needs.SelectMany(n => n.Places).ToList();
            foreach (var entry in want)
            {
                var parts = entry.Split('|');
                if (!places.Any(p => p.Tag == parts[0] && (parts.Length == 1 || p.Extra == parts[1])))
                {
                    differences.Add($"needs: no place {entry}");
                }
            }
        }

        void MissingNeedTexts(string[] want, AskNeed[] needs)
        {
            var texts = needs.Select(n => TextFolding.Fold(n.Text)).ToList();
            foreach (var fragment in want)
            {
                if (!texts.Any(t => t.Contains(TextFolding.Fold(fragment), StringComparison.Ordinal)))
                {
                    differences.Add($"needs: no need mentioning {fragment}");
                }
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
