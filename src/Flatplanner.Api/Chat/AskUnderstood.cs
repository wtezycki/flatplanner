using Flatplanner.Core.Search;

namespace Flatplanner.Api.Chat;

public sealed record AskUnderstood(
    string Summary,
    IReadOnlyList<string> Needs,
    IReadOnlyList<DeclinedNeed> Declined,
    IReadOnlyList<AskNotice> Notices) : AskEvent;
