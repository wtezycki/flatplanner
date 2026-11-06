using System.Globalization;
using System.Net;
using System.Text.Json;
using Flatplanner.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Flatplanner.Ingest.Enrichment;

public sealed class NearbyCommand(FlatplannerDbContext db, ILogger<NearbyCommand> logger) : IDisposable
{
    private const string OverpassUrl = "https://overpass-api.de/api/interpreter";
    private const string UserAgent = "Flatplanner-Ingest/1.0 (one-off local import)";
    private const int RadiusM = 1_500;
    private const int PlacesPerKind = 3;
    private const int BusyRetries = 5;
    private const double MetresPerDegreeLat = 111_320;

    // overpass-api.de allows 2 query slots per IP and answers 429 while both cool down.
    private static readonly TimeSpan BusyDelay = TimeSpan.FromSeconds(30);
    private static readonly string[] ExtraTags = ["isced:level", "religion", "denomination"];

    private static readonly (string Key, string[] Values)[] PlaceTags =
    [
        ("amenity", ["school", "kindergarten", "place_of_worship", "pharmacy", "clinic", "hospital", "doctors", "university"]),
        ("leisure", ["park", "playground", "dog_park"]),
        ("natural", ["wood"]),
        ("landuse", ["forest"]),
        ("railway", ["station", "halt", "tram_stop"]),
        ("highway", ["bus_stop"]),
        ("shop", ["supermarket", "convenience"]),
    ];

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
        try
        {
            places = await FetchPlacesAsync(BoundingBox(listings), cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Overpass failed, nearby_json left empty for {Count} listings", listings.Count);
            return;
        }

        foreach (var listing in listings)
        {
            var nearest = NearestPlaces(places, (double)listing.Lat, (double)listing.Lng);
            listing.NearbyJson = JsonSerializer.Serialize(nearest, DossierJson.Options);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Nearby: {Count} listings filled from {Places} OSM places", listings.Count, places.Count);
    }

    public void Dispose() => http.Dispose();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(200) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }

    private static string BoundingBox(IReadOnlyCollection<Listing> listings)
    {
        var south = (double)listings.Min(l => l.Lat);
        var north = (double)listings.Max(l => l.Lat);
        var latMargin = RadiusM / MetresPerDegreeLat;
        var lngMargin = RadiusM / (MetresPerDegreeLat * Math.Cos(north * Math.PI / 180));
        return string.Create(CultureInfo.InvariantCulture,
            $"{south - latMargin},{(double)listings.Min(l => l.Lng) - lngMargin},{north + latMargin},{(double)listings.Max(l => l.Lng) + lngMargin}");
    }

    private async Task<List<OsmPlace>> FetchPlacesAsync(string boundingBox, CancellationToken cancellationToken)
    {
        var filters = PlaceTags.Select(tag => $"nwr[{tag.Key}~\"^({string.Join('|', tag.Values)})$\"];");
        var query = $"[out:json][timeout:180][bbox:{boundingBox}];({string.Concat(filters)});out center bb tags;";

        using var response = await PostWithBusyRetryAsync(query, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.GetProperty("elements").EnumerateArray()
            .Select(ToOsmPlace)
            .OfType<OsmPlace>()
            .ToList();
    }

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

    // A global "nearest N" fills up with unnamed woods and playgrounds; a cap per kind keeps rarer places such as an Orthodox church (16.1).
    private static List<NearbyPlace> NearestPlaces(IEnumerable<OsmPlace> places, double lat, double lng) =>
        places
            .Select(place => new NearbyPlace(place.Name, place.Tag, place.Extra, (int)Math.Round(place.DistanceM(lat, lng))))
            .Where(place => place.DistanceM <= RadiusM)
            .GroupBy(place => place.Name is null ? (object)place : (place.Name, place.Tag))
            .Select(group => group.MinBy(place => place.DistanceM)!)
            .GroupBy(place => (place.Tag, place.Extra))
            .SelectMany(kind => kind.OrderBy(place => place.DistanceM).Take(PlacesPerKind))
            .OrderBy(place => place.DistanceM)
            .ToList();

    private static OsmPlace? ToOsmPlace(JsonElement element)
    {
        if (!element.TryGetProperty("tags", out var tags) || MatchTag(tags) is not { } tag)
        {
            return null;
        }

        var extras = ExtraTags
            .Select(key => tags.TryGetProperty(key, out var value) ? $"{key}={value.GetString()}" : null)
            .OfType<string>()
            .ToList();
        var name = tags.TryGetProperty("name", out var nameValue) ? nameValue.GetString() : null;
        var extra = extras.Count == 0 ? null : string.Join(';', extras);

        if (element.TryGetProperty("bounds", out var bounds))
        {
            return new OsmPlace(name, tag, extra,
                bounds.GetProperty("minlat").GetDouble(), bounds.GetProperty("minlon").GetDouble(),
                bounds.GetProperty("maxlat").GetDouble(), bounds.GetProperty("maxlon").GetDouble());
        }

        var point = element.TryGetProperty("center", out var center) ? center : element;
        if (!point.TryGetProperty("lat", out var lat) || !point.TryGetProperty("lon", out var lon))
        {
            return null;
        }

        return new OsmPlace(name, tag, extra, lat.GetDouble(), lon.GetDouble(), lat.GetDouble(), lon.GetDouble());
    }

    private static string? MatchTag(JsonElement tags)
    {
        foreach (var (key, values) in PlaceTags)
        {
            if (tags.TryGetProperty(key, out var value) && value.GetString() is { } tagValue && values.Contains(tagValue))
            {
                return $"{key}={tagValue}";
            }
        }

        return null;
    }

    // Areas are measured to the nearest point of their bounding box, so a large forest counts from its edge.
    private sealed record OsmPlace(string? Name, string Tag, string? Extra, double MinLat, double MinLng, double MaxLat, double MaxLng)
    {
        public double DistanceM(double lat, double lng) =>
            Haversine.DistanceM(lat, lng, Math.Clamp(lat, MinLat, MaxLat), Math.Clamp(lng, MinLng, MaxLng));
    }
}
