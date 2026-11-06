namespace Flatplanner.Core.Data;

public sealed record ListingRisk(string Kind, string Reason, string Quote, int? AmountPln = null);
