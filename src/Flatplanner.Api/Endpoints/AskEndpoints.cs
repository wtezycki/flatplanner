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
            var message = request.Message?.Trim() ?? "";
            var history = (request.History ?? []).Select(entry => entry.Trim()).ToList();
            if (AskValidation.Validate(message, history, options.Value.ChatMaxLength) is { } errors)
            {
                return Results.ValidationProblem(errors);
            }

            return Results.Json(
                await ask.AskAsync(message, history, request.ExcludeNeeds ?? [], filters.ToIntent(), request.More ?? false, ct),
                ApiJson.Options);
        });
    }
}
