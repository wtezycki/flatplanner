using Flatplanner.Core.Search;
using Flatplanner.Core.Text;

namespace Flatplanner.Api.Chat;

public static class EvidenceVerifier
{
    public static NeedResult Verify(JudgedNeed? judged, AskCandidate candidate)
    {
        if (judged is null || !Verdict.All.Contains(judged.Verdict))
        {
            return NeedResult.Unknown;
        }

        var verified = judged.Evidence
            .Select(e => (e.Kind, Label: Label(e, candidate)))
            .Where(e => e.Label is not null)
            .Select(e => new Evidence(e.Kind, e.Label!))
            .ToList();

        if (judged.Verdict != Verdict.Unknown && verified.Count == 0)
        {
            return NeedResult.Unknown;
        }

        var verdict = judged.Verdict == Verdict.Yes
            && candidate.Listing.LocationApproximate
            && verified.All(e => e.Kind == EvidenceKind.Place)
                ? Verdict.Partly
                : judged.Verdict;

        return new NeedResult(verdict, judged.Reason.Trim(), verified);
    }

    private static string? Label(JudgedEvidence evidence, AskCandidate candidate) => evidence.Kind switch
    {
        EvidenceKind.Quote => QuoteLabel(evidence.Ref, candidate),
        EvidenceKind.Place => PlaceLabel(evidence.Ref, candidate),
        EvidenceKind.Field => candidate.Fields.FirstOrDefault(f => f.Name == evidence.Ref)?.Label,
        _ => null,
    };

    private static string? PlaceLabel(string reference, AskCandidate candidate) =>
        int.TryParse(reference, out var index) && index >= 0 && index < candidate.Nearby.Count
            && candidate.Nearby[index] is var place
            ? PlaceLabels.Of(place.Name, place.Tag, place.DistanceM)
            : null;

    private static string? QuoteLabel(string quote, AskCandidate candidate)
    {
        var normalized = AnchorText.NormalizeQuote(quote);
        return normalized.Length > 0 && candidate.NormalizedDescription.Contains(normalized, StringComparison.Ordinal)
            ? quote.Trim()
            : null;
    }
}
