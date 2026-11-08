using Flatplanner.Core.Data;
using Flatplanner.Core.Search;
using Microsoft.EntityFrameworkCore;

namespace Flatplanner.Api.Listings;

public static class CardSignals
{
    private const int MedianMinGroupSize = 10;
    private const int MedianRounding = 100;
    private const int BelowMedianThresholdPercent = 10;
    private const int NearbyMaxDistanceM = 800;

    private static readonly string[] StopTags = ["railway=tram_stop", "railway=station", "railway=halt", "highway=bus_stop"];
    private const string ParkTag = "leisure=park";

    public static async Task<Dictionary<string, int>> CityMediansAsync(FlatplannerDbContext db, CancellationToken ct)
    {
        var rows = await db.Listings.AsNoTracking()
            .Select(l => new { l.CitySlug, l.PricePerM2, l.QualityFlags })
            .ToListAsync(ct);
        return rows
            .Where(r => !r.QualityFlags.Contains(QualityFlag.PriceOutlier))
            .GroupBy(r => r.CitySlug, r => r.PricePerM2)
            .Where(g => g.Count() >= MedianMinGroupSize)
            .ToDictionary(g => g.Key, g => Median(g.ToList()));
    }

    // Rounded to 100 zł, as the median of the "cheap" preference (design 7.4).
    public static int Median(IReadOnlyList<int> values)
    {
        var sorted = values.Order().ToList();
        var middle = sorted.Count / 2;
        var median = sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2.0;
        return (int)(Math.Round(median / MedianRounding, MidpointRounding.AwayFromZero) * MedianRounding);
    }

    public static int? BelowMedianPercent(int pricePerM2, int? median)
    {
        if (median is not > 0)
        {
            return null;
        }

        var percent = (int)Math.Round((1 - pricePerM2 / (double)median.Value) * 100, MidpointRounding.AwayFromZero);
        return percent >= BelowMedianThresholdPercent ? percent : null;
    }

    public static IReadOnlyList<string> RiskKinds(string? risksJson) => DossierJson.ParseList<ListingRisk>(risksJson)
        .Select(r => r.Kind)
        .Distinct()
        .OrderBy(kind => Array.IndexOf(RiskKind.All, kind))
        .ToList();

    public static string? NearestStop(string? nearbyJson, bool locationApproximate) =>
        Nearest(nearbyJson, locationApproximate, tag => StopTags.Contains(tag));

    public static string? NearestPark(string? nearbyJson, bool locationApproximate) =>
        Nearest(nearbyJson, locationApproximate, tag => tag == ParkTag);

    private static string? Nearest(string? nearbyJson, bool locationApproximate, Func<string, bool> isWanted) =>
        locationApproximate
            ? null
            : DossierJson.ParseList<NearbyPlace>(nearbyJson)
                .Where(p => p.DistanceM <= NearbyMaxDistanceM && isWanted(p.Tag))
                .MinBy(p => p.DistanceM) is { } place
                ? PlaceLabels.Of(null, place.Tag, place.DistanceM)
                : null;
}
