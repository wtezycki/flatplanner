using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flatplanner.Core.Data;

public class FlatplannerDbContext(DbContextOptions<FlatplannerDbContext> options) : DbContext(options)
{
    public DbSet<RawListing> RawListings => Set<RawListing>();

    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<ListingFact> ListingFacts => Set<ListingFact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasCharSet(DbConstants.CharSet, DelegationModes.ApplyToAll)
            .UseCollation(DbConstants.Collation, DelegationModes.ApplyToAll);

        modelBuilder.Entity<RawListing>()
            .HasIndex(r => new { r.Source, r.ExternalId })
            .IsUnique()
            .HasDatabaseName("ux_raw_listings_source_external");

        modelBuilder.Entity<Listing>(listing =>
        {
            listing.HasIndex(l => new { l.Source, l.ExternalId })
                .IsUnique()
                .HasDatabaseName("ux_listings_source_external");
            listing.HasIndex(l => new { l.CitySlug, l.DistrictSlug }).HasDatabaseName("ix_listings_city_district");
            listing.HasIndex(l => l.CreatedAtSource).HasDatabaseName("ix_listings_created");

            listing.Property(l => l.FactsStatus).HasDefaultValue(FactsStatus.Pending);
            listing.Property(l => l.ImageUrls).HasJsonArrayConversion();
            listing.Property(l => l.QualityFlags).HasJsonArrayConversion();

            listing.ToTable(t =>
            {
                t.HasCheckConstraint("ck_listings_has_lift", "has_lift IS NULL OR has_lift = 1");
                t.HasCheckConstraint("ck_listings_has_balcony", "has_balcony IS NULL OR has_balcony = 1");
                t.HasCheckConstraint("ck_listings_has_garage", "has_garage IS NULL OR has_garage = 1");
                t.HasCheckConstraint("ck_listings_has_basement", "has_basement IS NULL OR has_basement = 1");
            });
        });

        modelBuilder.Entity<ListingFact>(fact =>
        {
            fact.HasIndex(f => new { f.ListingId, f.FactKey })
                .IsUnique()
                .HasDatabaseName("ux_listing_facts_listing_key");
            fact.HasOne<Listing>()
                .WithMany()
                .HasForeignKey(f => f.ListingId)
                .HasConstraintName("fk_listing_facts_listing")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

internal static class JsonArrayPropertyExtensions
{
    public static void HasJsonArrayConversion(this PropertyBuilder<string[]> property) =>
        property.HasConversion(
            values => JsonSerializer.Serialize(values, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<string[]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<string>(),
            new ValueComparer<string[]>(
                (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.SequenceEqual(b)),
                values => values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value)),
                values => values.ToArray()));
}
