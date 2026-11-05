using Flatplanner.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Flatplanner.Core.Search;

public static class SearchMetaLoader
{
    public static async Task<SearchMeta> LoadAsync(FlatplannerDbContext db, CancellationToken ct)
    {
        var rows = await db.Listings
            .Select(l => new { l.CitySlug, l.DistrictSlug, l.DistrictName })
            .Distinct()
            .ToListAsync(ct);

        var citySlugs = City.All.Where(slug => rows.Any(r => r.CitySlug == slug)).ToList();
        var districts = rows
            .Where(r => r.DistrictSlug != null && r.DistrictName != null)
            .Select(r => new SearchMetaDistrict($"{r.CitySlug}:{r.DistrictSlug}", r.CitySlug, r.DistrictName!))
            .OrderBy(d => d.Key, StringComparer.Ordinal)
            .ToList();

        return new SearchMeta(citySlugs, districts, City.Gdansk);
    }
}
