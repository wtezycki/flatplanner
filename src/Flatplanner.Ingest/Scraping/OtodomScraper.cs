using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Flatplanner.Core.Data;
using Flatplanner.Ingest.Normalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Flatplanner.Ingest.Scraping;

public sealed class OtodomScraper(FlatplannerDbContext db, ILogger<OtodomScraper> logger) : IDisposable
{
    private const string UserAgent =
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36";
    private const string RawDirectory = "data/raw/otodom";
    private const int PageLimit = 36;

    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(45)];
    private static readonly string[] ContactKeys = ["owner", "agency", "contactDetails", "userAdverts"];

    private static readonly (string City, string Market, int Quota)[] Quotas =
    [
        (City.Gdansk, Market.Primary, 55), (City.Gdansk, Market.Secondary, 55),
        (City.Gdynia, Market.Primary, 30), (City.Gdynia, Market.Secondary, 30),
        (City.Sopot, Market.Primary, 15), (City.Sopot, Market.Secondary, 15),
    ];

    private readonly HttpClient http = CreateHttpClient();
    private readonly ScrapeCounters counters = new();

    public async Task<ScrapeCounters> RunAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(RawDirectory);
        foreach (var (city, market, quota) in Quotas)
        {
            await ScrapeSegmentAsync(city, market, quota, cancellationToken);
        }

        return counters;
    }

    public void Dispose() => http.Dispose();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }

    private async Task ScrapeSegmentAsync(string city, string market, int quota, CancellationToken cancellationToken)
    {
        var seen = new HashSet<long>();
        var saved = 0;
        for (var page = 1; saved < quota; page++)
        {
            var results = await FetchResultsPageAsync(city, market, page, cancellationToken);
            var candidates = results.Where(item => seen.Add(item.Id)).ToList();
            if (candidates.Count == 0)
            {
                break;
            }

            foreach (var candidate in candidates.TakeWhile(_ => saved < quota))
            {
                if (await SaveListingAsync(candidate.Slug, cancellationToken))
                {
                    saved++;
                }
            }
        }

        counters.Saved[(city, market)] = saved;
    }

    private async Task<List<SearchItem>> FetchResultsPageAsync(string city, string market, int page, CancellationToken cancellationToken)
    {
        var url = $"https://www.otodom.pl/pl/wyniki/sprzedaz/mieszkanie/pomorskie/{city}/{city}/{city}" +
                  $"?by=LATEST&direction=DESC&market={market.ToUpperInvariant()}&limit={PageLimit}&page={page}";
        using var data = NextData.Parse(await FetchAsync(url, cancellationToken))
            ?? throw new ScrapeBlockedException($"No __NEXT_DATA__ on results page {url}");

        var items = new List<SearchItem>();
        JsonElement? root = data.RootElement;
        foreach (var element in root.Get("props", "pageProps", "data", "searchAds", "items").Items())
        {
            JsonElement? item = element;
            if (item.Get("estate").GetString() != "FLAT" || item.Get("hidePrice") is { ValueKind: JsonValueKind.True })
            {
                continue;
            }

            if (item.Get("id") is { ValueKind: JsonValueKind.Number } id && item.Get("slug").GetString() is { } slug)
            {
                items.Add(new SearchItem(id.GetInt64(), slug));
            }
        }

        return items;
    }

    private async Task<bool> SaveListingAsync(string slug, CancellationToken cancellationToken)
    {
        using var data = NextData.Parse(await FetchAsync($"https://www.otodom.pl/pl/oferta/{slug}", cancellationToken));
        JsonElement? root = data?.RootElement;
        if (root.Get("props", "pageProps", "ad") is not { ValueKind: JsonValueKind.Object } ad)
        {
            return Skip(slug);
        }

        var node = JsonNode.Parse(ad.GetRawText())!.AsObject();
        foreach (var key in ContactKeys)
        {
            node.Remove(key);
        }

        var externalId = node["id"]?.ToString();
        var url = node["url"]?.GetValue<string>();
        if (externalId is null || url is null)
        {
            return Skip(slug);
        }

        var json = node.ToJsonString();
        await File.WriteAllTextAsync(Path.Combine(RawDirectory, $"{externalId}.json"), json, cancellationToken);

        var raw = await db.RawListings.SingleOrDefaultAsync(
            r => r.Source == Source.Otodom && r.ExternalId == externalId, cancellationToken);
        if (raw is null)
        {
            raw = new RawListing { Source = Source.Otodom, ExternalId = externalId, Url = url, AdJson = json };
            db.RawListings.Add(raw);
        }
        else
        {
            raw.Url = url;
            raw.AdJson = json;
        }

        raw.FetchedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private bool Skip(string slug)
    {
        logger.LogWarning("No ad node {Slug}", slug);
        counters.Skipped++;
        return false;
    }

    private async Task<string> FetchAsync(string url, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5 + Random.Shared.NextDouble() * 1.5), cancellationToken);
            counters.Requests++;
            using var response = await http.GetAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new ScrapeBlockedException($"403 for {url}");
            }

            var retryable = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            if (retryable && attempt < RetryDelays.Length)
            {
                counters.Retries++;
                logger.LogWarning("Retrying {Url} after {Status}", url, (int)response.StatusCode);
                await Task.Delay(RetryDelays[attempt], cancellationToken);
                continue;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }

    private sealed record SearchItem(long Id, string Slug);
}
