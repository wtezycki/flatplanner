using System.Text.RegularExpressions;
using Flatplanner.Core.Data;
using Flatplanner.Core.Search;
using Flatplanner.Core.Text;

namespace Flatplanner.Api.Chat;

public static partial class JudgeInput
{
    private const int DescriptionMaxLength = 8000;
    private const string RoutePrefix = "route=";

    // A need the planner could not place gets the whole dossier, so it loses nothing (design 16.8).
    private static bool NeedsFullDossier(IReadOnlyList<AskNeed> needs) =>
        needs.Any(n => n.Places.Length == 0 && n.Keywords.Length == 0);

    // Indexes stay those of nearby_json, because EvidenceVerifier resolves `place` refs against it.
    public static IReadOnlyList<JudgePlace> Nearby(IReadOnlyList<NearbyPlace> nearby, IReadOnlyList<AskNeed> needs)
    {
        var planned = needs.SelectMany(n => n.Places).ToList();
        var full = NeedsFullDossier(needs);
        // A named place can be the third of three sports centres ("Kyokushin Karate"); a full dossier keeps one per kind to stay small (design 16.9).
        var perKind = full ? 1 : NearbyPlace.MaxPerKind;

        return nearby
            .Select((place, index) => (Place: place, Index: index))
            .Where(p => full || planned.Any(n => n.Accepts(p.Place)))
            .GroupBy(p => (p.Place.Tag, p.Place.Extra))
            .SelectMany(kind => kind.OrderBy(p => p.Place.DistanceM).Take(perKind))
            .OrderBy(p => p.Index)
            .Select(p => new JudgePlace(
                p.Index,
                p.Place.Tag.StartsWith(RoutePrefix, StringComparison.Ordinal) ? null : p.Place.Name,
                p.Place.Tag,
                p.Place.Extra,
                p.Place.DistanceM))
            .ToList();
    }

    // Without a citable place or sentence the verifier could only leave `unknown`, so the listing is not sent (design 16.9).
    public static bool HasContext(AskCandidate candidate, IReadOnlyList<AskNeed> needs) =>
        NeedsFullDossier(needs)
        || Nearby(candidate.Nearby, needs).Count > 0
        || Description(candidate.Listing.DescriptionText, needs).Length > 0;

    public static string Description(string description, IReadOnlyList<AskNeed> needs)
    {
        var text = NeedsFullDossier(needs)
            ? description
            : string.Join('\n', SentenceBreak().Split(description)
                .Select(s => s.Trim())
                .Where(s => Mentions(TextFolding.Fold(s), needs)));

        return text.Length > DescriptionMaxLength ? text[..DescriptionMaxLength] : text;
    }

    private static bool Mentions(string foldedSentence, IReadOnlyList<AskNeed> needs) =>
        needs.Any(n => n.Mentions(foldedSentence));

    [GeneratedRegex(@"(?<=[.!?])\s+|[\r\n]+")]
    private static partial Regex SentenceBreak();
}
