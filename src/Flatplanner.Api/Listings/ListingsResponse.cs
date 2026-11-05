namespace Flatplanner.Api.Listings;

public sealed record ListingsResponse(
    IReadOnlyList<ListingItem> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    string Sort,
    Interpretation Interpretation);
