namespace Flatplanner.Api.Chat;

public sealed record AskRequest(string? Message, int[]? ExcludeNeeds);
