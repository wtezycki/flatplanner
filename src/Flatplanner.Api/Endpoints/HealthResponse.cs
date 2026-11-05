namespace Flatplanner.Api.Endpoints;

public sealed record HealthResponse(string Status, string Db, string? ClientIp);
