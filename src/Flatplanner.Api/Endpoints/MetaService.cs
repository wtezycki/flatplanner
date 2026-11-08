using Flatplanner.Core.Data;
using Flatplanner.Core.Options;
using Flatplanner.Core.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Flatplanner.Api.Endpoints;

public sealed class MetaService(FlatplannerDbContext db, IOptions<FlatplannerOptions> options)
{
    public async Task<MetaResponse> GetAsync(CancellationToken ct)
    {
        var cityCounts = await db.Listings
            .GroupBy(l => l.CitySlug)
            .Select(g => new { Slug = g.Key, Count = g.Count() })
            .ToDictionaryAsync(c => c.Slug, c => c.Count, ct);

        var districts = await db.Listings
            .Where(l => l.DistrictSlug != null && l.DistrictName != null)
            .GroupBy(l => new { l.CitySlug, l.DistrictSlug, l.DistrictName })
            .Select(g => new { g.Key.CitySlug, g.Key.DistrictSlug, g.Key.DistrictName, Count = g.Count() })
            .OrderBy(d => d.DistrictName)
            .ToListAsync(ct);

        var cities = City.All
            .Select(slug => new CityMeta(
                slug,
                EnumLabels.CityNames[slug],
                cityCounts.GetValueOrDefault(slug),
                districts
                    .Where(d => d.CitySlug == slug)
                    .Select(d => new DistrictMeta($"{slug}:{d.DistrictSlug}", d.DistrictName!, d.Count))
                    .ToList()))
            .ToList();

        var dataAsOf = await db.Listings.MaxAsync(l => (DateTime?)l.ScrapedAt, ct);

        var settings = options.Value;
        return new MetaResponse(
            cities,
            City.Gdansk,
            new MetaEnums(
                EnumLabels.MarketOptions,
                EnumLabels.AdvertiserTypeOptions,
                EnumLabels.FinishConditionOptions,
                EnumLabels.OwnershipOptions,
                EnumLabels.HeatingOptions,
                EnumLabels.BuildingTypeOptions,
                EnumLabels.WindowsTypeOptions,
                EnumLabels.BuildingMaterialOptions,
                EnumLabels.FactStatusOptions,
                EnumLabels.SortOptions,
                EnumLabels.ProvenanceOptions),
            settings.ChatMode,
            settings.ChatMaxLength,
            SearchDefinitions.MaxTurns,
            dataAsOf is { } value ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : null);
    }
}
