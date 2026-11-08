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
        DossierJson.ParseList<NearbyPlace>(listing.NearbyJson),
        DossierJson.ParseList<ListingRisk>(listing.RisksJson),
        AnchorText.Normalize(listing.DescriptionText));
}
