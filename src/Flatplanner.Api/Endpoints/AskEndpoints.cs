using Flatplanner.Api.Chat;
using Flatplanner.Api.Listings;
using Flatplanner.Core.Options;
using Microsoft.Extensions.Options;

namespace Flatplanner.Api.Endpoints;

public static class AskEndpoints
{
    public static void MapAsk(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/search/ask", async (
            AskRequest request,
            [AsParameters] ListingsQuery filters,
            AskService ask,
            IOptions<FlatplannerOptions> options,
            CancellationToken ct) =>
        {
            var (message, history) = Normalize(request);
            if (AskValidation.Validate(message, history, options.Value.ChatMaxLength) is { } errors)
            {
                return Results.ValidationProblem(errors);
            }

            return Results.Json(
                await ask.AskAsync(message, history, request.ExcludeNeeds ?? [], filters.ToIntent(), request.More ?? false, ct),
                ApiJson.Options);
        });

        app.MapPost("/api/search/ask/stream", IResult (
            AskRequest request,
            [AsParameters] ListingsQuery filters,
            AskService ask,
            IOptions<FlatplannerOptions> options,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var (message, history) = Normalize(request);
            if (AskValidation.Validate(message, history, options.Value.ChatMaxLength) is { } errors)
            {
                return Results.ValidationProblem(errors);
            }

            var events = ask.AskStreamAsync(message, history, request.ExcludeNeeds ?? [], filters.ToIntent(), request.More ?? false, ct);
            return new AskStreamResult(events, loggerFactory.CreateLogger("Flatplanner.Api.Ask"));
        });
    }

    private static (string Message, List<string> History) Normalize(AskRequest request) =>
        (request.Message?.Trim() ?? "", (request.History ?? []).Select(entry => entry.Trim()).ToList());
}
