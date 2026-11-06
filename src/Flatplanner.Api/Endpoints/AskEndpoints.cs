using Flatplanner.Api.Chat;
using Flatplanner.Core.Options;
using Microsoft.Extensions.Options;

namespace Flatplanner.Api.Endpoints;

public static class AskEndpoints
{
    public static void MapAsk(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/search/ask", async (
            AskRequest request,
            AskService ask,
            IOptions<FlatplannerOptions> options,
            CancellationToken ct) =>
        {
            var message = request.Message?.Trim() ?? "";
            var maxLength = options.Value.ChatMaxLength;
            if (message.Length == 0 || message.Length > maxLength)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["message"] = [$"Wpisz od 1 do {maxLength} znaków."],
                });
            }

            return Results.Json(await ask.AskAsync(message, request.ExcludeNeeds ?? [], ct), ApiJson.Options);
        });
    }
}
