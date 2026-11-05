using Flatplanner.Api.Endpoints;
using Flatplanner.Core.Data;
using Flatplanner.Core.Search;
using Microsoft.EntityFrameworkCore;

namespace Flatplanner.Api.Listings;

public sealed class ListingsService(FlatplannerDbContext db)
{
    public async Task<ListingsResponse> SearchAsync(ListingsQuery request, CancellationToken ct)
    {
        var meta = await SearchMetaLoader.LoadAsync(db, ct);
        var (intent, warnings) = SearchIntentNormalizer.Normalize(request.ToIntent(), meta);
        var interpretation = InterpretationBuilder.Build(intent, meta, warnings);
        var sort = ResolveSort(request.Sort);

        var filtered = db.Listings.AsNoTracking().Apply(intent);
        var total = await filtered.CountAsync(ct);
        var totalPages = (total + SearchDefinitions.PageSize - 1) / SearchDefinitions.PageSize;
        var page = Math.Clamp(request.Page ?? 1, 1, Math.Max(totalPages, 1));

        var rows = await Sort(filtered, sort)
                .Skip((page - 1) * SearchDefinitions.PageSize)
                .Take(SearchDefinitions.PageSize)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.PricePln,
                    l.AreaM2,
                    l.PricePerM2,
                    l.Rooms,
                    l.Floor,
                    l.BuildingFloors,
                    l.CitySlug,
                    l.DistrictName,
                    l.ImageUrls,
                    l.Market,
                    l.CreatedAtSource,
                    l.LocationApproximate,
                    l.QualityFlags,
                })
                .ToListAsync(ct);

        var items = rows.Select(r => new ListingItem(
            r.Id, r.Title, r.PricePln, r.AreaM2, r.PricePerM2, r.Rooms, r.Floor,
            r.QualityFlags.Contains(QualityFlag.FloorHigher10), r.BuildingFloors,
            r.CitySlug, EnumLabels.CityNames[r.CitySlug], r.DistrictName,
            r.ImageUrls, r.Market, DateTime.SpecifyKind(r.CreatedAtSource, DateTimeKind.Utc),
            r.LocationApproximate, r.QualityFlags.Contains(QualityFlag.PriceOutlier))).ToList();

        return new ListingsResponse(items, page, SearchDefinitions.PageSize, total, totalPages, sort, interpretation);
    }

    public async Task<ListingDetail?> GetAsync(long id, CancellationToken ct)
    {
        var listing = await db.Listings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        return listing is null
            ? null
            : new ListingDetail(
                listing.Id, listing.Url, listing.Title,
                DateTime.SpecifyKind(listing.ScrapedAt, DateTimeKind.Utc),
                DateTime.SpecifyKind(listing.CreatedAtSource, DateTimeKind.Utc),
                listing.ImageUrls, listing.DescriptionText, listing.CitySlug, EnumLabels.CityNames[listing.CitySlug],
                listing.DistrictName, listing.Street, listing.LocationApproximate,
                listing.PricePln, listing.AreaM2, listing.PricePerM2, listing.Rooms, listing.Floor, listing.QualityFlags.Contains(QualityFlag.FloorHigher10),
                listing.QualityFlags.Contains(QualityFlag.FloorGarret), listing.BuildingFloors, listing.BuildYear,
                listing.Market, listing.FinishCondition, listing.Ownership, listing.RentPln,
                listing.HasLift, listing.HasBalcony, listing.HasGarage, listing.HasBasement, listing.AdvertiserType);
    }

    // `relevance` ranks by preferences (design 7.5), which arrives with item #8; until then it falls back to newest.
    private static string ResolveSort(string? requested) =>
        requested is SortOrder.PriceAsc or SortOrder.PriceDesc or SortOrder.PricePerM2Asc ? requested : SortOrder.Newest;

    private static IOrderedQueryable<Listing> Sort(IQueryable<Listing> query, string sort) => sort switch
    {
        SortOrder.PriceAsc => query.OrderBy(l => l.PricePln).ThenByDescending(l => l.Id),
        SortOrder.PriceDesc => query.OrderByDescending(l => l.PricePln).ThenByDescending(l => l.Id),
        SortOrder.PricePerM2Asc => query.OrderBy(l => l.PricePerM2).ThenByDescending(l => l.Id),
        _ => query.OrderByDescending(l => l.CreatedAtSource).ThenByDescending(l => l.Id),
    };
}
