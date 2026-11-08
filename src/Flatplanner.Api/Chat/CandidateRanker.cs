using Flatplanner.Core.Data;
using Flatplanner.Core.Search;
using Flatplanner.Core.Text;

namespace Flatplanner.Api.Chat;

public static class CandidateRanker
{
    private const double KeywordBonus = 0.5;

    public static IReadOnlyList<AskCandidate> Rank(IEnumerable<AskCandidate> candidates, IReadOnlyList<AskNeed> needs) => candidates
        .OrderByDescending(c => needs.Sum(n => Score(c, n)))
        .ThenByDescending(c => c.Listing.CreatedAtSource)
        .ThenByDescending(c => c.Listing.Id)
        .ToList();

    private static double Score(AskCandidate candidate, AskNeed need)
    {
        var present = need.Places
            .Select(p => (p.Prefer, Score: PlaceScore(candidate.Nearby, p)))
            .Where(p => p.Score.HasValue)
            .ToLookup(p => p.Prefer, p => p.Score.GetValueOrDefault());
        var bestNear = present[PlacePreference.Near].DefaultIfEmpty(0).Max();
        // The nearest noise source decides, so `far` takes the worst present tag (design 16.9).
        var worstFar = present[PlacePreference.Far].DefaultIfEmpty(0).Min();
        var place = Math.Max(bestNear, worstFar);
        var mentioned = need.Keywords.Length > 0
            && (JudgeInput.Description(candidate.Listing.DescriptionText, [need]).Length > 0 || NamesMention(candidate.Nearby, need));
        return place + (mentioned ? KeywordBonus : 0);
    }

    // OSM tags a martial arts club only as a sports centre, so its name ("Karate Klub") is the one clue; every flat has some sports centre nearby (design 16.9).
    private static bool NamesMention(IReadOnlyList<NearbyPlace> nearby, AskNeed need)
    {
        var near = need.Places.Where(p => p.Prefer == PlacePreference.Near).ToList();
        return nearby.Any(p => p.Name is not null && near.Any(n => n.Accepts(p)) && need.Mentions(TextFolding.Fold(p.Name)));
    }

    // A tag absent within the radius scores 0 either way: absence is not citable evidence (design 16.9).
    private static double? PlaceScore(IReadOnlyList<NearbyPlace> nearby, NeedPlace place)
    {
        var nearest = nearby.Where(place.Accepts).MinBy(p => p.DistanceM);
        if (nearest is null)
        {
            return null;
        }

        var share = Math.Min(nearest.DistanceM, NearbyPlace.RadiusM) / (double)NearbyPlace.RadiusM;
        return place.Prefer == PlacePreference.Near ? 1 - share : share;
    }
}
