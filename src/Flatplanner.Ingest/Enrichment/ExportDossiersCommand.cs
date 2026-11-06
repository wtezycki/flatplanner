using System.Text.Json;
using Flatplanner.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Flatplanner.Ingest.Enrichment;

public sealed class ExportDossiersCommand(FlatplannerDbContext db, ILogger<ExportDossiersCommand> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var listings = await db.Listings.AsNoTracking().OrderBy(l => l.Id).ToListAsync(cancellationToken);
        var lines = listings.Select(l => JsonSerializer.Serialize(new
        {
            l.ExternalId,
            Fields = new
            {
                l.Title,
                City = l.CitySlug,
                l.DistrictName,
                l.Street,
                l.PricePln,
                l.AreaM2,
                l.PricePerM2,
                l.Rooms,
                l.Floor,
                l.BuildingFloors,
                l.BuildYear,
                l.Market,
                l.FinishCondition,
                l.Ownership,
                l.Heating,
                l.BuildingType,
                l.WindowsType,
                l.BuildingMaterial,
                l.RentPln,
                l.HasLift,
                l.HasBalcony,
                l.HasGarage,
                l.HasBasement,
                l.AdvertiserType,
            },
            l.DescriptionText,
        }, DossierJson.Options));

        Directory.CreateDirectory(Path.GetDirectoryName(DossierFiles.ListingsPath)!);
        await File.WriteAllLinesAsync(DossierFiles.ListingsPath, lines, cancellationToken);
        logger.LogInformation("Exported {Count} listings to {Path}", listings.Count, DossierFiles.ListingsPath);
    }
}
