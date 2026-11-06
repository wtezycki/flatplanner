using Flatplanner.Core.Data;
using Flatplanner.Ingest.Enrichment;
using Microsoft.EntityFrameworkCore;

namespace Flatplanner.Ingest.Reporting;

public sealed class ReportCommand(FlatplannerDbContext db)
{
    private const string ReportPath = "docs/ingest-report.md";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var raws = await db.RawListings.AsNoTracking().ToListAsync(cancellationToken);
        var listings = await db.Listings.AsNoTracking().ToListAsync(cancellationToken);
        var dossiers = File.Exists(DossierFiles.DossiersPath)
            ? DossierValidation.Run(
                await File.ReadAllLinesAsync(DossierFiles.DossiersPath, cancellationToken),
                listings.ToDictionary(l => l.ExternalId, l => l.DescriptionText))
            : null;
        var report = ReportBuilder.Build(raws, listings, dossiers);

        Console.Write(report);
        await File.WriteAllTextAsync(ReportPath, report, cancellationToken);
    }
}
