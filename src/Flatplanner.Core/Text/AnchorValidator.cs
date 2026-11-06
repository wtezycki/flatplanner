using System.Globalization;
using System.Text.RegularExpressions;
using Flatplanner.Core.Data;

namespace Flatplanner.Core.Text;

public static partial class AnchorValidator
{
    private const int MinExtraCostPln = 5_000;
    private const int MaxExtraCostPln = 300_000;

    public static string? Validate(ListingRisk risk, string normalizedDescription)
    {
        if (!RiskKind.All.Contains(risk.Kind))
        {
            return RiskRejectReason.UnknownKind;
        }

        if (string.IsNullOrWhiteSpace(risk.Reason))
        {
            return RiskRejectReason.MissingReason;
        }

        if (string.IsNullOrWhiteSpace(risk.Quote))
        {
            return RiskRejectReason.MissingQuote;
        }

        var quote = AnchorText.NormalizeQuote(risk.Quote);
        if (!normalizedDescription.Contains(quote, StringComparison.Ordinal))
        {
            return RiskRejectReason.QuoteNotFound;
        }

        if (risk.AmountPln is not { } amount)
        {
            return null;
        }

        if (risk.Kind != RiskKind.ExtraCost)
        {
            return RiskRejectReason.AmountNotAllowed;
        }

        if (!NumbersIn(quote).Contains(amount))
        {
            return RiskRejectReason.AmountNotInQuote;
        }

        return amount is < MinExtraCostPln or > MaxExtraCostPln ? RiskRejectReason.AmountOutOfRange : null;
    }

    private static IEnumerable<long> NumbersIn(string normalizedQuote) =>
        Number().Matches(normalizedQuote).Select(match =>
        {
            var value = double.Parse(match.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
            var multiplier = match.Groups[2].Value switch
            {
                "mln" => 1_000_000,
                "" => 1,
                _ => 1_000,
            };
            return (long)Math.Round(value * multiplier);
        });

    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(tys\.?|tys|k|mln)?")]
    private static partial Regex Number();
}
