using Flatplanner.Api.Listings;

namespace Flatplanner.Api.Endpoints;

public static class ListingsEndpoints
{
    public static void MapListings(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/listings", (
            [AsParameters] ListingsQuery query,
            ListingsService listings,
            CancellationToken ct) => listings.SearchAsync(query, ct));

        app.MapGet("/api/listings/{id:long}", async (long id, ListingsService listings, CancellationToken ct) =>
            await listings.GetAsync(id, ct) is { } detail
                ? Results.Json(detail, ApiJson.Options)
                : Results.Problem(statusCode: StatusCodes.Status404NotFound));
    }
}
