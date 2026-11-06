using System.Text.Json;
using Flatplanner.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Flatplanner.Ingest.Enrichment;

public sealed class ImportDossiersCommand(FlatplannerDbContext db, ILogger<ImportDossiersCommand> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var listings = await db.Listings.ToListAsync(cancellationToken);
        var lines = await File.ReadAllLinesAsync(DossierFiles.DossiersPath, cancellationToken);
        var validation = DossierValidation.Run(lines, listings.ToDictionary(l => l.ExternalId, l => l.DescriptionText));

        foreach (var invalid in validation.InvalidLines)
        {
            logger.LogWarning("Invalid dossier line {Line}", invalid);
        }

        foreach (var listing in listings)
        {
            if (validation.Valid.TryGetValue(listing.ExternalId, out var dossier))
            {
                listing.AiSummary = dossier.Summary;
                listing.RisksJson = JsonSerializer.Serialize(dossier.Risks, DossierJson.Options);
                listing.FactsStatus = FactsStatus.Done;
            }
            else
            {
                listing.AiSummary = null;
                listing.RisksJson = null;
                listing.FactsStatus = FactsStatus.Failed;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Imported dossiers: {Done} done, {Failed} failed, {Dropped} risks dropped",
            validation.Valid.Count, listings.Count - validation.Valid.Count, validation.DroppedRisks.Count);
    }
}
