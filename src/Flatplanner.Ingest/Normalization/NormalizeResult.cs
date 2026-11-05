using Flatplanner.Core.Data;

namespace Flatplanner.Ingest.Normalization;

public sealed record NormalizeResult(Listing? Listing, string? RejectReason)
{
    public static NormalizeResult Accept(Listing listing) => new(listing, null);

    public static NormalizeResult Reject(string reason) => new(null, reason);
}
