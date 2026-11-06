using System.Text.Json;
using Flatplanner.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Flatplanner.Ingest.Normalization;

public sealed class NormalizeCommand(FlatplannerDbContext db, ILogger<NormalizeCommand> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var existing = await db.Listings.ToDictionaryAsync(l => (l.Source, l.ExternalId), cancellationToken);
        var accepted = 0;
        var rejected = 0;

        foreach (var raw in await db.RawListings.ToListAsync(cancellationToken))
        {
            using var document = JsonDocument.Parse(raw.AdJson);
            var scrapedAt = DateTime.SpecifyKind(raw.FetchedAt, DateTimeKind.Utc);
            var result = ListingNormalizer.Normalize(document.RootElement, scrapedAt);

            if (result.Listing is null)
            {
                raw.NormalizeStatus = NormalizeStatus.Rejected;
                raw.RejectReason = result.RejectReason;
                logger.LogWarning("Rejected {ExternalId}: {Reason}", raw.ExternalId, result.RejectReason);
                rejected++;
                continue;
            }

            raw.NormalizeStatus = NormalizeStatus.Accepted;
            raw.RejectReason = null;
            if (existing.TryGetValue((raw.Source, raw.ExternalId), out var current))
            {
                await UpdateAsync(current, result.Listing, cancellationToken);
            }
            else
            {
                db.Listings.Add(result.Listing);
            }

            accepted++;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Normalized: {Accepted} accepted, {Rejected} rejected", accepted, rejected);
    }

    private async Task UpdateAsync(Listing current, Listing fresh, CancellationToken cancellationToken)
    {
        var descriptionChanged = current.DescriptionText != fresh.DescriptionText;
        var locationChanged = current.Lat != fresh.Lat || current.Lng != fresh.Lng;
        fresh.Id = current.Id;
        fresh.FactsStatus = current.FactsStatus;
        fresh.AiSummary = current.AiSummary;
        fresh.RisksJson = current.RisksJson;
        fresh.NearbyJson = locationChanged ? null : current.NearbyJson;
        db.Entry(current).CurrentValues.SetValues(fresh);

        if (descriptionChanged)
        {
            current.FactsStatus = FactsStatus.Pending;
            current.AiSummary = null;
            current.RisksJson = null;
            db.ListingFacts.RemoveRange(await db.ListingFacts.Where(f => f.ListingId == current.Id).ToListAsync(cancellationToken));
        }
    }
}
