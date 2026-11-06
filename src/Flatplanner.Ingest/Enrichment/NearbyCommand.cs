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
    private const int MaxPlaces = 60;

    private const int BusyRetries = 5;

    private static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(1);
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
        var filled = 0;
        foreach (var listing in listings)
        {
            try
            {
                var places = await FetchPlacesAsync((double)listing.Lat, (double)listing.Lng, cancellationToken);
                listing.NearbyJson = JsonSerializer.Serialize(places, DossierJson.Options);
                await db.SaveChangesAsync(cancellationToken);
                filled++;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Overpass failed for {ExternalId}", listing.ExternalId);
            }

            await Task.Delay(RequestInterval, cancellationToken);
        }

        logger.LogInformation("Nearby: {Filled}/{Total} listings filled", filled, listings.Count);
    }

    public void Dispose() => http.Dispose();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }

    private async Task<List<NearbyPlace>> FetchPlacesAsync(double lat, double lng, CancellationToken cancellationToken)
    {
        using var response = await PostWithBusyRetryAsync(BuildQuery(lat, lng), cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return ToPlaces(document.RootElement.GetProperty("elements"), lat, lng);
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

    private static string BuildQuery(double lat, double lng)
    {
        var around = string.Create(CultureInfo.InvariantCulture, $"around:{RadiusM},{lat},{lng}");
        var filters = PlaceTags.Select(tag => $"nwr({around})[{tag.Key}~\"^({string.Join('|', tag.Values)})$\"];");
        return $"[out:json][timeout:25];({string.Concat(filters)});out center tags;";
    }

    private static List<NearbyPlace> ToPlaces(JsonElement elements, double lat, double lng) =>
        elements.EnumerateArray()
            .Select(element => ToPlace(element, lat, lng))
            .OfType<NearbyPlace>()
            .GroupBy(place => place.Name is null ? (object)place : (place.Name, place.Tag))
            .Select(group => group.MinBy(place => place.DistanceM)!)
            .OrderBy(place => place.DistanceM)
            .Take(MaxPlaces)
            .ToList();

    private static NearbyPlace? ToPlace(JsonElement element, double lat, double lng)
    {
        if (!element.TryGetProperty("tags", out var tags))
        {
            return null;
        }

        var point = element.TryGetProperty("center", out var center) ? center : element;
        if (!point.TryGetProperty("lat", out var placeLat) || !point.TryGetProperty("lon", out var placeLng))
        {
            return null;
        }

        if (MatchTag(tags) is not { } tag)
        {
            return null;
        }

        var extras = ExtraTags
            .Select(key => tags.TryGetProperty(key, out var value) ? $"{key}={value.GetString()}" : null)
            .OfType<string>()
            .ToList();
        var name = tags.TryGetProperty("name", out var nameValue) ? nameValue.GetString() : null;
        var distance = Haversine.DistanceM(lat, lng, placeLat.GetDouble(), placeLng.GetDouble());

        return new NearbyPlace(name, tag, extras.Count == 0 ? null : string.Join(';', extras), (int)Math.Round(distance));
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
}
