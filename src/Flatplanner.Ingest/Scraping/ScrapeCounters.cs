namespace Flatplanner.Ingest.Scraping;

public sealed class ScrapeCounters
{
    public int Requests { get; set; }

    public int Retries { get; set; }

    public int Skipped { get; set; }

    public SortedDictionary<(string City, string Market), int> Saved { get; } = [];

    public void Write(TextWriter output)
    {
        output.WriteLine($"requests: {Requests}, retries: {Retries}, skipped: {Skipped}");
        foreach (var ((city, market), count) in Saved)
        {
            output.WriteLine($"saved {city} {market}: {count}");
        }
    }
}
