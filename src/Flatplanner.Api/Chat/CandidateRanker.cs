using Flatplanner.Core.Data;
using Flatplanner.Core.Search;

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
        var place = need.Places
            .Select(p => PlaceScore(candidate.Nearby, p))
            .DefaultIfEmpty(0)
            .Max();
        var keyword = need.Keywords.Length > 0 && JudgeInput.Description(candidate.Listing.DescriptionText, [need]).Length > 0
            ? KeywordBonus
            : 0;
        return Math.Min(1, place + keyword);
    }

    // A tag absent within the radius scores 0 either way: absence is not citable evidence (design 16.9).
    private static double PlaceScore(IReadOnlyList<NearbyPlace> nearby, NeedPlace place)
    {
        var nearest = nearby.Where(p => p.Tag == place.Tag).MinBy(p => p.DistanceM);
        if (nearest is null)
        {
            return 0;
        }

        var share = Math.Min(nearest.DistanceM, NearbyPlace.RadiusM) / (double)NearbyPlace.RadiusM;
        return place.Prefer == PlacePreference.Near ? 1 - share : share;
    }
}
