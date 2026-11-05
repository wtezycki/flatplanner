using System.Globalization;
using System.Text;
using System.Text.Json;
using Flatplanner.Core.Data;
using Flatplanner.Core.Text;
using Flatplanner.Ingest.Normalization;

namespace Flatplanner.Ingest.Reporting;

public static class ReportBuilder
{
    private const int CheapRankingSize = 20;
    private const int DuplicateExamples = 10;
    private const string Unknown = "unknown";
    private const string NoValue = "(null)";

    private static readonly (string Column, Func<Listing, bool> IsFilled)[] NullableColumns =
    [
        ("district_slug", l => l.DistrictSlug is not null),
        ("district_name", l => l.DistrictName is not null),
        ("street", l => l.Street is not null),
        ("floor", l => l.Floor is not null),
        ("building_floors", l => l.BuildingFloors is not null),
        ("build_year", l => l.BuildYear is not null),
        ("finish_condition", l => l.FinishCondition is not null),
        ("ownership", l => l.Ownership is not null),
        ("rent_pln", l => l.RentPln is not null),
        ("has_lift", l => l.HasLift is not null),
        ("has_balcony", l => l.HasBalcony is not null),
        ("has_garage", l => l.HasGarage is not null),
        ("has_basement", l => l.HasBasement is not null),
        ("advertiser_type", l => l.AdvertiserType is not null),
    ];

    public static string Build(IReadOnlyList<RawListing> raws, IReadOnlyList<Listing> listings)
    {
        var report = new StringBuilder("# Ingest report\n");
        AppendFetched(report, raws);
        AppendRejected(report, raws);
        AppendFillRate(report, listings);
        AppendFlags(report, listings);
        AppendRent(report, listings);
        AppendFinishByMarket(report, listings);
        AppendDistricts(report, listings);
        AppendMedians(report, listings);
        AppendDuplicates(report, listings);
        AppendCheapRanking(report, listings);
        return report.ToString();
    }

    private static void AppendFetched(StringBuilder report, IReadOnlyList<RawListing> raws)
    {
        var counts = raws
            .Select(raw => ReadCityAndMarket(raw.AdJson))
            .GroupBy(key => key)
            .OrderBy(group => group.Key.City).ThenBy(group => group.Key.Market);

        report.AppendLine("\n## Fetched\n").AppendLine($"Total: {raws.Count}\n");
        AppendTable(report, ["city", "market", "n"], counts.Select(g => new[] { g.Key.City, g.Key.Market, g.Count().ToString(CultureInfo.InvariantCulture) }));
    }

    private static (string City, string Market) ReadCityAndMarket(string adJson)
    {
        using var document = JsonDocument.Parse(adJson);
        JsonElement? ad = document.RootElement;
        var city = ad.Get("target", "City").GetString();
        var market = ad.Get("target", "MarketType").GetString();
        return (city is null ? Unknown : TextFolding.Fold(city), market?.ToLowerInvariant() ?? Unknown);
    }

    private static void AppendRejected(StringBuilder report, IReadOnlyList<RawListing> raws)
    {
        var rejected = raws.Where(r => r.NormalizeStatus == NormalizeStatus.Rejected)
            .GroupBy(r => r.RejectReason ?? Unknown)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key);

        report.AppendLine("\n## Rejected\n");
        AppendTable(report, ["reason", "n"], rejected.Select(g => new[] { g.Key, g.Count().ToString(CultureInfo.InvariantCulture) }));
    }

    private static void AppendFillRate(StringBuilder report, IReadOnlyList<Listing> listings)
    {
        report.AppendLine("\n## Fill-rate\n").AppendLine("Share of listings with a value, `n (%)`.\n");
        var cities = City.All;
        var header = new[] { "column" }.Concat(cities).Append("total").ToArray();
        var rows = NullableColumns.Select(column =>
            new[] { column.Column }
                .Concat(cities.Select(city => FillCell(listings.Where(l => l.CitySlug == city).ToList(), column.IsFilled)))
                .Append(FillCell(listings, column.IsFilled))
                .ToArray());
        AppendTable(report, header, rows);
    }

    private static string FillCell(IReadOnlyCollection<Listing> listings, Func<Listing, bool> isFilled) =>
        listings.Count == 0
            ? "-"
            : $"{listings.Count(isFilled)}/{listings.Count} ({Percent(listings.Count(isFilled), listings.Count)})";

    private static void AppendFlags(StringBuilder report, IReadOnlyList<Listing> listings)
    {
        var flags = listings.SelectMany(l => l.QualityFlags).GroupBy(f => f).OrderByDescending(g => g.Count()).ThenBy(g => g.Key);
        report.AppendLine("\n## Flags\n");
        AppendTable(report, ["flag", "n"], flags.Select(g => new[] { g.Key, g.Count().ToString(CultureInfo.InvariantCulture) }));
    }

    private static void AppendRent(StringBuilder report, IReadOnlyList<Listing> listings)
    {
        var rows = Market.All.Select(market =>
        {
            var inMarket = listings.Where(l => l.Market == market).ToList();
            var zero = inMarket.Count(l => l.QualityFlags.Contains(QualityFlag.RentZeroAsMissing));
            return new[] { market, $"{zero}/{inMarket.Count}", inMarket.Count == 0 ? "-" : Percent(zero, inMarket.Count) };
        });

        report.AppendLine("\n## Rent (czynsz)\n").AppendLine("Listings with `Rent = 0` stored as missing (`rent_zero_as_missing`).\n");
        AppendTable(report, ["market", "zero / all", "share"], rows);
    }

    private static void AppendFinishByMarket(StringBuilder report, IReadOnlyList<Listing> listings)
    {
        var conditions = FinishCondition.All.Cast<string?>().Append(null);
        var rows = conditions.Select(condition =>
            new[] { condition ?? NoValue }
                .Concat(Market.All.Select(market =>
                    listings.Count(l => l.FinishCondition == condition && l.Market == market).ToString(CultureInfo.InvariantCulture)))
                .ToArray());

        report.AppendLine("\n## Finish condition by market\n");
        AppendTable(report, new[] { "finish_condition" }.Concat(Market.All).ToArray(), rows);
    }

    private static void AppendDistricts(StringBuilder report, IReadOnlyList<Listing> listings)
    {
        report.AppendLine("\n## Districts\n");
        var withDistrict = listings.Where(l => l.DistrictSlug is not null).ToList();
        AppendTable(report, ["city", "unique districts"], City.All.Select(city =>
            new[] { city, withDistrict.Where(l => l.CitySlug == city).Select(l => l.DistrictSlug).Distinct().Count().ToString(CultureInfo.InvariantCulture) }));

        var single = withDistrict
            .GroupBy(l => (l.CitySlug, l.DistrictSlug))
            .Where(g => g.Count() == 1)
            .OrderBy(g => g.Key.CitySlug).ThenBy(g => g.Key.DistrictSlug)
            .Select(g => $"{g.Key.CitySlug}/{g.Key.DistrictSlug}");
        report.AppendLine("\nDistricts with a single listing (manual review): " + string.Join(", ", single));
    }

    private static void AppendMedians(StringBuilder report, IReadOnlyList<Listing> listings)
    {
        report.AppendLine("\n## Median price per m²\n");
        AppendTable(report, ["city", "n", "median price_per_m2"], City.All.Select(city =>
        {
            var prices = listings.Where(l => l.CitySlug == city).Select(l => l.PricePerM2).ToList();
            return new[] { city, prices.Count.ToString(CultureInfo.InvariantCulture), prices.Count == 0 ? "-" : Median(prices).ToString("0", CultureInfo.InvariantCulture) };
        }));
    }

    private static void AppendDuplicates(StringBuilder report, IReadOnlyList<Listing> listings)
    {
        var pairs = new List<(Listing A, Listing B)>();
        for (var i = 0; i < listings.Count; i++)
        {
            for (var j = i + 1; j < listings.Count; j++)
            {
                if (IsDuplicateCandidate(listings[i], listings[j]))
                {
                    pairs.Add((listings[i], listings[j]));
                }
            }
        }

        var inPairs = pairs.SelectMany(p => new[] { p.A.Id, p.B.Id }).Distinct().Count();
        report.AppendLine("\n## Duplicate candidates\n")
            .AppendLine($"Pairs: {pairs.Count}; listings in at least one pair: {inPairs}/{listings.Count} ({(listings.Count == 0 ? "-" : Percent(inPairs, listings.Count))}).\n");
        AppendTable(report, ["external_id A", "advertiser A", "external_id B", "advertiser B"], pairs.Take(DuplicateExamples).Select(p =>
            new[] { p.A.ExternalId, p.A.AdvertiserType ?? NoValue, p.B.ExternalId, p.B.AdvertiserType ?? NoValue }));
    }

    private static bool IsDuplicateCandidate(Listing a, Listing b) =>
        a.DistrictSlug is not null
        && a.DistrictSlug == b.DistrictSlug
        && a.CitySlug == b.CitySlug
        && a.Rooms == b.Rooms
        && Math.Abs(a.AreaM2 - b.AreaM2) <= 0.5m
        && a.Floor == b.Floor
        && Math.Abs(a.PricePln - b.PricePln) <= 0.03m * Math.Max(a.PricePln, b.PricePln);

    private static void AppendCheapRanking(StringBuilder report, IReadOnlyList<Listing> listings)
    {
        var medians = City.All
            .Where(city => listings.Any(l => l.CitySlug == city))
            .ToDictionary(city => city, city => Median(listings.Where(l => l.CitySlug == city).Select(l => l.PricePerM2).ToList()));
        var cheapest = listings
            .OrderBy(l => l.PricePerM2 / medians[l.CitySlug])
            .Take(CheapRankingSize)
            .ToList();

        report.AppendLine("\n## Primary market in the cheap ranking\n")
            .AppendLine($"Primary share overall: {PrimaryShare(listings)}; among the {cheapest.Count} lowest `price_per_m2 / city median`: {PrimaryShare(cheapest)}.");
    }

    private static string PrimaryShare(IReadOnlyCollection<Listing> listings) =>
        listings.Count == 0 ? "-" : $"{Percent(listings.Count(l => l.Market == Market.Primary), listings.Count)} ({listings.Count(l => l.Market == Market.Primary)}/{listings.Count})";

    private static string Percent(int part, int whole) =>
        (100.0 * part / whole).ToString("0.#", CultureInfo.InvariantCulture) + "%";

    private static double Median(IReadOnlyList<int> values)
    {
        var sorted = values.Order().ToArray();
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2.0;
    }

    private static void AppendTable(StringBuilder report, string[] header, IEnumerable<string[]> rows)
    {
        report.AppendLine("| " + string.Join(" | ", header) + " |");
        report.AppendLine("|" + string.Concat(header.Select(_ => "---|")));
        foreach (var row in rows)
        {
            report.AppendLine("| " + string.Join(" | ", row) + " |");
        }
    }
}
