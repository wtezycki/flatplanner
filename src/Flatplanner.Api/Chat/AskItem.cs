using Flatplanner.Api.Listings;

namespace Flatplanner.Api.Chat;

public sealed record AskItem(
    ListingItem Listing,
    string Group,
    IReadOnlyList<NeedResult> Needs,
    int? TotalWithExtrasPln);
