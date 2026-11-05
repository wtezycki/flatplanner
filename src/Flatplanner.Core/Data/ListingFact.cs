using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flatplanner.Core.Data;

[Table("listing_facts")]
public class ListingFact
{
    [Column("id")]
    public long Id { get; set; }

    [Column("listing_id")]
    public long ListingId { get; set; }

    [Column("fact_key")]
    [MaxLength(16)]
    public required string FactKey { get; set; }

    [Column("status")]
    [MaxLength(16)]
    public required string Status { get; set; }

    [Column("amount_pln")]
    public int? AmountPln { get; set; }

    [Column("quote")]
    [MaxLength(400)]
    public string? Quote { get; set; }

    [Column("validation_status")]
    [MaxLength(16)]
    public required string ValidationStatus { get; set; }

    [Column("reject_reason")]
    [MaxLength(32)]
    public string? RejectReason { get; set; }

    [Column("model")]
    [MaxLength(64)]
    public required string Model { get; set; }

    [Column("prompt_version")]
    [MaxLength(32)]
    public required string PromptVersion { get; set; }

    [Column("extracted_at")]
    public DateTime ExtractedAt { get; set; }
}
