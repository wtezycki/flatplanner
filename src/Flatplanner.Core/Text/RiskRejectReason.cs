namespace Flatplanner.Core.Text;

public static class RiskRejectReason
{
    public const string UnknownKind = "unknown_kind";
    public const string MissingReason = "missing_reason";
    public const string MissingQuote = "missing_quote";
    public const string QuoteNotFound = "quote_not_found";
    public const string AmountNotAllowed = "amount_not_allowed";
    public const string AmountNotInQuote = "amount_not_in_quote";
    public const string AmountOutOfRange = "amount_out_of_range";
}
