using Flatplanner.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Flatplanner.Ingest.Reporting;

public sealed class ReportCommand(FlatplannerDbContext db)
{
    private const string ReportPath = "docs/ingest-report.md";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var raws = await db.RawListings.AsNoTracking().ToListAsync(cancellationToken);
        var listings = await db.Listings.AsNoTracking().ToListAsync(cancellationToken);
        var report = ReportBuilder.Build(raws, listings);

        Console.Write(report);
        await File.WriteAllTextAsync(ReportPath, report, cancellationToken);
    }
}
