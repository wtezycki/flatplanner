using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Flatplanner.Api;

public static class RateLimiting
{
    public const string ChatPolicy = "chat";

    private const int GlobalPermitLimit = 120;
    private const int ChatPermitLimit = 5;
    private const string UnknownClient = "unknown";
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddFlatplannerRateLimiter(this IServiceCollection services) =>
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                http.Request.Path.StartsWithSegments("/api")
                    ? RateLimitPartition.GetFixedWindowLimiter(Client(http), _ => FixedWindow(GlobalPermitLimit))
                    : RateLimitPartition.GetNoLimiter(string.Empty));
            limiter.AddPolicy(ChatPolicy, http =>
                RateLimitPartition.GetFixedWindowLimiter(Client(http), _ => FixedWindow(ChatPermitLimit)));
            limiter.OnRejected = WriteRejectionAsync;
        });

    private static string Client(HttpContext http) => ClientIp.Of(http) ?? UnknownClient;

    private static FixedWindowRateLimiterOptions FixedWindow(int permitLimit) =>
        new() { PermitLimit = permitLimit, Window = Window, QueueLimit = 0 };

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken ct)
    {
        var http = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            http.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await http.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = StatusCodes.Status429TooManyRequests, Title = "Too Many Requests" },
            options: null,
            contentType: "application/problem+json",
            cancellationToken: ct);
    }
}
