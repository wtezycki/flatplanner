using Flatplanner.Api.Listings;

namespace Flatplanner.Api.Endpoints;

public static class ListingsEndpoints
{
    public static void MapListings(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/listings", (
            [AsParameters] ListingsQuery query,
            ListingsService listings,
            CancellationToken ct) => listings.SearchAsync(query, ct));
}
