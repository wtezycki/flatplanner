using Flatplanner.Core.Search;

namespace Flatplanner.Api.Chat;

public sealed record AskEvaluation(
    SearchIntent Intent,
    IReadOnlyList<string> Needs,
    IReadOnlyList<DeclinedNeed> Declined,
    IReadOnlyList<AskNotice> Notices,
    string? FallbackReason,
    IReadOnlyList<AskCandidate> Candidates,
    IReadOnlyDictionary<long, IReadOnlyList<NeedResult>> Results,
    bool JudgeFailed);
