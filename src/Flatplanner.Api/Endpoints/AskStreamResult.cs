using System.Text.Json;
using Flatplanner.Api.Chat;

namespace Flatplanner.Api.Endpoints;

// Server-sent events written by hand: the framework helper arrives only in .NET 10.
public sealed class AskStreamResult(IAsyncEnumerable<AskEvent> events, ILogger logger) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var ct = httpContext.RequestAborted;
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await foreach (var ev in events.WithCancellation(ct))
            {
                var (name, payload) = ev is AskResults results ? ("results", (object)results.Response) : ("understood", ev);
                await WriteAsync(httpContext, name, JsonSerializer.Serialize(payload, payload.GetType(), ApiJson.Options), ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The ask stream failed");
            await WriteAsync(httpContext, "error", "{}", ct);
        }
    }

    private static async Task WriteAsync(HttpContext httpContext, string name, string json, CancellationToken ct)
    {
        await httpContext.Response.WriteAsync($"event: {name}\ndata: {json}\n\n", ct);
        await httpContext.Response.Body.FlushAsync(ct);
    }
}
