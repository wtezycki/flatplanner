using Flatplanner.Core.Search;

namespace Flatplanner.Api.Chat;

public sealed record AskResponse(
    string Summary,
    IReadOnlyList<string> Needs,
    IReadOnlyList<DeclinedNeed> Declined,
    IReadOnlyList<AskNotice> Notices,
    IReadOnlyList<AskItem> Items,
    int UnknownCount,
    IReadOnlyList<Relaxation> Relaxations,
    bool UsedFallback,
    string? FallbackReason);
