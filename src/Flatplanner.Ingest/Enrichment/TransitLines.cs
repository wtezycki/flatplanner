using System.Text.Json;
using Flatplanner.Core.Data;

namespace Flatplanner.Ingest.Enrichment;

public static class TransitLines
{
    public static List<TransitRoute> Parse(JsonElement elements)
    {
        var nodes = new Dictionary<long, (double Lat, double Lng)>();
        foreach (var node in elements.EnumerateArray().Where(e => e.GetProperty("type").GetString() == "node" && e.TryGetProperty("lat", out _)))
        {
            nodes.TryAdd(node.GetProperty("id").GetInt64(), (node.GetProperty("lat").GetDouble(), node.GetProperty("lon").GetDouble()));
        }

        return elements.EnumerateArray()
            .Where(e => e.GetProperty("type").GetString() == "relation")
            .Select(relation => ToRoute(relation, nodes))
            .OfType<TransitRoute>()
            .ToList();
    }

    // One entry per line, both directions merged, measured to its nearest stop or platform.
    public static IEnumerable<NearbyPlace> Nearest(IEnumerable<TransitRoute> routes, double lat, double lng, int radiusM) =>
        routes
            .Select(route => (Route: route, DistanceM: route.Stops.Min(stop => Haversine.DistanceM(lat, lng, stop.Lat, stop.Lng))))
            .Where(line => line.DistanceM <= radiusM)
            .GroupBy(line => (line.Route.Tag, line.Route.Ref))
            .Select(group => group.MinBy(line => line.DistanceM))
            .Select(line => new NearbyPlace(line.Route.Label, line.Route.Tag, $"ref={line.Route.Ref}", (int)Math.Round(line.DistanceM)));

    private static TransitRoute? ToRoute(JsonElement relation, IReadOnlyDictionary<long, (double Lat, double Lng)> nodes)
    {
        if (!relation.TryGetProperty("tags", out var tags) || !relation.TryGetProperty("members", out var members))
        {
            return null;
        }

        var name = Tag(tags, "name");
        if ((Tag(tags, "ref") ?? name) is not { } lineRef)
        {
            return null;
        }

        var stops = members.EnumerateArray()
            .Where(member => member.GetProperty("type").GetString() == "node")
            .Select(member => nodes.TryGetValue(member.GetProperty("ref").GetInt64(), out var stop) ? stop : ((double, double)?)null)
            .OfType<(double Lat, double Lng)>()
            .ToArray();
        if (stops.Length == 0)
        {
            return null;
        }

        var label = Tag(tags, "from") is { } from && Tag(tags, "to") is { } to ? $"{lineRef}: {from} → {to}" : name ?? lineRef;
        return new TransitRoute($"route={Tag(tags, "route")}", lineRef, label, stops);
    }

    private static string? Tag(JsonElement tags, string key) => tags.TryGetProperty(key, out var value) ? value.GetString() : null;
}
