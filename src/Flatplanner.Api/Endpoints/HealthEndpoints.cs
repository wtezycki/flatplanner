using System.Data.Common;
using Flatplanner.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flatplanner.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealth(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/health", CheckAsync);

    private static async Task<IResult> CheckAsync(
        FlatplannerDbContext db,
        HttpContext http,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var clientIp = http.Request.Headers["X-Real-IP"].FirstOrDefault() ?? http.Connection.RemoteIpAddress?.ToString();
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            return Results.Ok(new HealthResponse("ok", "ok", clientIp));
        }
        catch (Exception ex) when (ex is DbException or RetryLimitExceededException)
        {
            logger.LogError(ex, "Database health check failed");
            return Results.Json(new HealthResponse("error", "error", clientIp), ApiJson.Options, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
