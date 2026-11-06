namespace Flatplanner.Api.Chat;

public sealed record JudgedListing(long Id, IReadOnlyList<JudgedNeed> Needs);
