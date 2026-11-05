using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flatplanner.Core.Data;

[Table("raw_listings")]
public class RawListing
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

    [Column("fetched_at")]
    public DateTime FetchedAt { get; set; }

    [Column("ad_json", TypeName = "json")]
    public required string AdJson { get; set; }

    [Column("normalize_status")]
    [MaxLength(16)]
    public string? NormalizeStatus { get; set; }

    [Column("reject_reason")]
    [MaxLength(64)]
    public string? RejectReason { get; set; }
}
