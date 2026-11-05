using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flatplanner.Core.Data;

[Table("listings")]
public class Listing
{
    [Column("id")]
    public long Id { get; set; }

    [Column("source")]
    [MaxLength(16)]
    public required string Source { get; set; }

    [Column("external_id")]
    [MaxLength(32)]
    public required string ExternalId { get; set; }

    [Column("url")]
    [MaxLength(512)]
    public required string Url { get; set; }

    [Column("title")]
    [MaxLength(300)]
    public required string Title { get; set; }

    [Column("description_text", TypeName = "mediumtext")]
    public required string DescriptionText { get; set; }

    [Column("search_text", TypeName = "mediumtext")]
    public required string SearchText { get; set; }

    [Column("city_slug")]
    [MaxLength(16)]
    public required string CitySlug { get; set; }

    [Column("district_slug")]
    [MaxLength(80)]
    public string? DistrictSlug { get; set; }

    [Column("district_name")]
    [MaxLength(120)]
    public string? DistrictName { get; set; }

    [Column("street")]
    [MaxLength(160)]
    public string? Street { get; set; }

    [Column("lat", TypeName = "decimal(9,6)")]
    public decimal Lat { get; set; }

    [Column("lng", TypeName = "decimal(9,6)")]
    public decimal Lng { get; set; }

    [Column("location_approximate")]
    public bool LocationApproximate { get; set; }

    [Column("price_pln")]
    public int PricePln { get; set; }

    [Column("area_m2", TypeName = "decimal(7,2)")]
    public decimal AreaM2 { get; set; }

    [Column("price_per_m2")]
    public int PricePerM2 { get; set; }

    [Column("rooms")]
    public byte Rooms { get; set; }

    [Column("floor")]
    public sbyte? Floor { get; set; }

    [Column("building_floors")]
    public byte? BuildingFloors { get; set; }

    [Column("build_year")]
    public short? BuildYear { get; set; }

    [Column("market")]
    [MaxLength(16)]
    public required string Market { get; set; }

    [Column("finish_condition")]
    [MaxLength(24)]
    public string? FinishCondition { get; set; }

    [Column("ownership")]
    [MaxLength(24)]
    public string? Ownership { get; set; }

    [Column("rent_pln")]
    public int? RentPln { get; set; }

    [Column("has_lift")]
    public bool? HasLift { get; set; }

    [Column("has_balcony")]
    public bool? HasBalcony { get; set; }

    [Column("has_garage")]
    public bool? HasGarage { get; set; }

    [Column("has_basement")]
    public bool? HasBasement { get; set; }

    [Column("advertiser_type")]
    [MaxLength(16)]
    public string? AdvertiserType { get; set; }

    [Column("image_urls", TypeName = "json")]
    public string[] ImageUrls { get; set; } = [];

    [Column("quality_flags", TypeName = "json")]
    public string[] QualityFlags { get; set; } = [];

    [Column("facts_status")]
    [MaxLength(16)]
    public string FactsStatus { get; set; } = Data.FactsStatus.Pending;

    [Column("created_at_source")]
    public DateTime CreatedAtSource { get; set; }

    [Column("scraped_at")]
    public DateTime ScrapedAt { get; set; }
}
