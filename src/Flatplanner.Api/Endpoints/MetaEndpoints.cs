namespace Flatplanner.Api.Endpoints;

public static class MetaEndpoints
{
    public static void MapMeta(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/meta", (MetaService meta, CancellationToken ct) => meta.GetAsync(ct));
}
