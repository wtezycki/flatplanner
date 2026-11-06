namespace Flatplanner.Api.Chat;

public sealed record JudgeOutcome(IReadOnlyDictionary<long, IReadOnlyList<JudgedNeed>> Judgments, bool AnyBatchFailed);
