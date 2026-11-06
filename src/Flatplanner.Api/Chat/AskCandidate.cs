using System.Text.Json;
using Flatplanner.Core.Data;
using Flatplanner.Core.Text;

namespace Flatplanner.Api.Chat;

public sealed record AskCandidate(
    Listing Listing,
    IReadOnlyList<ListingField> Fields,
    IReadOnlyList<NearbyPlace> Nearby,
    IReadOnlyList<ListingRisk> Risks,
    string NormalizedDescription)
{
    public static AskCandidate From(Listing listing) => new(
        listing,
        ListingFields.Of(listing),
        Parse<NearbyPlace>(listing.NearbyJson),
        Parse<ListingRisk>(listing.RisksJson),
        AnchorText.Normalize(listing.DescriptionText));

    private static IReadOnlyList<T> Parse<T>(string? json) =>
        json is null ? [] : JsonSerializer.Deserialize<T[]>(json, DossierJson.Options) ?? [];
}
