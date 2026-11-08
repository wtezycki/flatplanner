using System.Globalization;
using System.Net;
using System.Text.Json;
using Flatplanner.Core.Data;
using Flatplanner.Ingest.Normalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Flatplanner.Ingest.Enrichment;

public sealed class NearbyCommand(FlatplannerDbContext db, ILogger<NearbyCommand> logger) : IDisposable
{
    private const string OverpassUrl = "https://overpass-api.de/api/interpreter";
    private const string UserAgent = "Flatplanner-Ingest/1.0 (one-off local import)";
    private const int TransitRadiusM = 500;
    private const int LandmarkMarginM = 20_000;
    private const int PlacesPerKind = 3;
    private const int BusyRetries = 5;
    private const double MetresPerDegreeLat = 111_320;

    // overpass-api.de allows 2 query slots per IP and answers 429 while both cool down.
    private static readonly TimeSpan BusyDelay = TimeSpan.FromSeconds(30);
    private static readonly string[] ExtraTags = ["isced:level", "religion", "denomination"];

    private static readonly (string Key, string[] Values)[] PlaceTags =
    [
        ("amenity",
        [
            "school", "kindergarten", "place_of_worship", "pharmacy", "clinic", "hospital", "doctors", "university",
            "veterinary", "bar", "pub", "nightclub", "restaurant", "cafe", "charging_station", "bicycle_rental",
        ]),
        ("leisure", ["park", "playground", "dog_park", "fitness_centre", "sports_centre", "swimming_pool", "stadium"]),
        ("natural", ["wood", "beach"]),
        ("landuse", ["forest", "cemetery"]),
        ("railway", ["station", "halt", "tram_stop"]),
        ("highway", ["bus_stop"]),
        ("shop", ["supermarket", "convenience", "mall"]),
    ];

    private static readonly (string Key, string[] Values)[] LineTags =
    [
        ("highway", ["motorway", "trunk", "primary"]),
        ("railway", ["rail", "tram"]),
    ];

    private static readonly (string Key, string[] Values)[] LandmarkTags =
    [
        ("place", ["city", "town"]),
        ("aeroway", ["aerodrome"]),
    ];

    // Kept at any distance, so "how far to the sea / the centre / the airport" has an answer beyond 1 500 m.
    private static readonly string[] AlwaysNearestTags =
        ["natural=beach", "railway=station", "railway=halt", "amenity=hospital", "aeroway=aerodrome", "place=city", "place=town"];

    private readonly HttpClient http = CreateHttpClient();

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var listings = await db.Listings.Where(l => l.NearbyJson == null).ToListAsync(cancellationToken);
        if (listings.Count == 0)
        {
            logger.LogInformation("Nearby: no listings without nearby_json");
            return;
        }

        List<OsmPlace> places;
        List<TransitRoute> routes;
        try
        {
            places = await FetchPlacesAsync(BoundingBox(listings, NearbyPlace.RadiusM), cancellationToken);
            using var transit = await FetchJsonAsync(TransitAndLandmarksQuery(listings), cancellationToken);
            var elements = transit.RootElement.GetProperty("elements");
            routes = TransitLines.Parse(elements);
            places.AddRange(elements.EnumerateArray().Select(ToOsmPlace).OfType<OsmPlace>());
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Overpass failed, nearby_json left empty for {Count} listings", listings.Count);
            return;
        }

        foreach (var listing in listings)
        {
            var nearest = NearestPlaces(places, routes, (double)listing.Lat, (double)listing.Lng);
            listing.NearbyJson = JsonSerializer.Serialize(nearest, DossierJson.Options);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Nearby: {Count} listings filled from {Places} OSM places and {Routes} transit routes", listings.Count, places.Count, routes.Count);
    }

    public void Dispose() => http.Dispose();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(200) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }

    private static string BoundingBox(IReadOnlyCollection<Listing> listings, int marginM)
    {
        var south = (double)listings.Min(l => l.Lat);
        var north = (double)listings.Max(l => l.Lat);
        var latMargin = marginM / MetresPerDegreeLat;
        var lngMargin = marginM / (MetresPerDegreeLat * Math.Cos(north * Math.PI / 180));
        return string.Create(CultureInfo.InvariantCulture,
            $"{south - latMargin},{(double)listings.Min(l => l.Lng) - lngMargin},{north + latMargin},{(double)listings.Max(l => l.Lng) + lngMargin}");
    }

    private async Task<List<OsmPlace>> FetchPlacesAsync(string boundingBox, CancellationToken cancellationToken)
    {
        var places = string.Concat(PlaceTags.Select(tag => $"nwr{Filter(tag)};"));
        var lines = string.Concat(LineTags.Select(tag => $"way{Filter(tag)};"));
        var query = $"[out:json][timeout:180][bbox:{boundingBox}];({places});out geom;({lines});out geom tags;";

        using var document = await FetchJsonAsync(query, cancellationToken);
        return document.RootElement.GetProperty("elements").EnumerateArray()
            .Select(ToOsmPlace)
            .OfType<OsmPlace>()
            .ToList();
    }

    private static string TransitAndLandmarksQuery(IReadOnlyCollection<Listing> listings)
    {
        var routes = $"rel[type=route][route~\"^(bus|trolleybus|tram|train|light_rail)$\"]({BoundingBox(listings, TransitRadiusM)})->.r;.r out body;node(r.r);out skel;";
        var wide = BoundingBox(listings, LandmarkMarginM);
        var cities = string.Join('|', ListingNormalizer.CityNames.Values);
        var landmarks = $"node[place~\"^(city|town)$\"][name~\"^({cities})$\"]({wide});out;nwr[aeroway=aerodrome][iata]({wide});out center;";
        return $"[out:json][timeout:180];{routes}{landmarks}";
    }

    private async Task<JsonDocument> FetchJsonAsync(string query, CancellationToken cancellationToken)
    {
        using var response = await PostWithBusyRetryAsync(query, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static string Filter((string Key, string[] Values) tag) => $"[{tag.Key}~\"^({string.Join('|', tag.Values)})$\"]";

    private async Task<HttpResponseMessage> PostWithBusyRetryAsync(string query, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var content = new FormUrlEncodedContent([new("data", query)]);
            var response = await http.PostAsync(OverpassUrl, content, cancellationToken);
            if (attempt == BusyRetries || response.StatusCode is not (HttpStatusCode.TooManyRequests or HttpStatusCode.GatewayTimeout))
            {
                return response;
            }

            response.Dispose();
            logger.LogWarning("Overpass busy ({Attempt}/{Retries}), waiting {Delay}", attempt, BusyRetries, BusyDelay);
            await Task.Delay(BusyDelay, cancellationToken);
        }
    }

    private static List<NearbyPlace> NearestPlaces(IEnumerable<OsmPlace> places, IEnumerable<TransitRoute> routes, double lat, double lng)
    {
        var measured = places
            .Select(place => new NearbyPlace(place.Name, place.Tag, place.Extra, (int)Math.Round(place.DistanceM(lat, lng))))
            .ToList();
        var landmarks = measured
            .Where(place => AlwaysNearestTags.Contains(place.Tag))
            .GroupBy(place => place.Tag.StartsWith("place=", StringComparison.Ordinal) ? place.Name : place.Tag)
            .Select(group => group.MinBy(place => place.DistanceM)!);

        return WithinRadius(measured)
            .Concat(landmarks)
            .Concat(TransitLines.Nearest(routes, lat, lng, TransitRadiusM))
            .Distinct()
            .OrderBy(place => place.DistanceM)
            .ToList();
    }

    // A global "nearest N" fills up with unnamed woods and playgrounds; a cap per kind keeps rarer places such as an Orthodox church (16.1).
    private static IEnumerable<NearbyPlace> WithinRadius(IEnumerable<NearbyPlace> measured) =>
        measured
            .Where(place => place.DistanceM <= NearbyPlace.RadiusM)
            .GroupBy(place => place.Name is null && !IsLine(place.Tag) ? (object)place : (place.Name, place.Tag))
            .Select(group => group.MinBy(place => place.DistanceM)!)
            .GroupBy(place => (place.Tag, place.Extra))
            .SelectMany(kind => kind.OrderBy(place => place.DistanceM).Take(PlacesPerKind));

    private static OsmPlace? ToOsmPlace(JsonElement element)
    {
        if (!element.TryGetProperty("tags", out var tags) || (MatchTag(tags, PlaceTags) ?? MatchTag(tags, LineTags) ?? MatchTag(tags, LandmarkTags)) is not { } tag)
        {
            return null;
        }

        var extras = ExtraTags
            .Select(key => tags.TryGetProperty(key, out var value) ? $"{key}={value.GetString()}" : null)
            .OfType<string>()
            .ToList();
        var name = tags.TryGetProperty("name", out var nameValue) ? nameValue.GetString() : null;
        var extra = extras.Count == 0 ? null : string.Join(';', extras);

        if (Geometry(element) is { Length: > 0 } lines)
        {
            return new OsmPlace(name, tag, extra, lines);
        }

        var point = element.TryGetProperty("center", out var center) ? center : element;
        if (!point.TryGetProperty("lat", out var lat) || !point.TryGetProperty("lon", out var lon))
        {
            return null;
        }

        return new OsmPlace(name, tag, extra, [[(lat.GetDouble(), lon.GetDouble())]]);
    }

    // A way carries its geometry, a relation the geometry of every member way.
    private static (double Lat, double Lng)[][] Geometry(JsonElement element)
    {
        IEnumerable<JsonElement> geometries = [];
        if (element.TryGetProperty("geometry", out var way))
        {
            geometries = [way];
        }
        else if (element.TryGetProperty("members", out var members))
        {
            geometries = members.EnumerateArray().Where(m => m.TryGetProperty("geometry", out _)).Select(m => m.GetProperty("geometry"));
        }

        return geometries
            .Select(g => g.EnumerateArray().Select(node => (node.GetProperty("lat").GetDouble(), node.GetProperty("lon").GetDouble())).ToArray())
            .Where(line => line.Length > 0)
            .ToArray();
    }

    private static bool IsLine(string tag) => LineTags.Any(line => line.Values.Any(value => tag == $"{line.Key}={value}"));

    private static string? MatchTag(JsonElement tags, (string Key, string[] Values)[] candidates)
    {
        foreach (var (key, values) in candidates)
        {
            if (tags.TryGetProperty(key, out var value) && value.GetString() is { } tagValue && values.Contains(tagValue))
            {
                return $"{key}={tagValue}";
            }
        }

        return null;
    }

    // Measured to the geometry, not to the bounding box: the box of a long beach or a large park covers listings far from it (design 16.1).
    private sealed record OsmPlace(string? Name, string Tag, string? Extra, (double Lat, double Lng)[][] Lines)
    {
        public double DistanceM(double lat, double lng) => Lines.Min(line => Haversine.DistanceToPolylineM(lat, lng, line));
    }
}
