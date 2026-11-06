using Flatplanner.Core.Data;

namespace Flatplanner.Ingest.Enrichment;

public sealed record DossierLine(string? ExternalId, string? Summary, ListingRisk[]? Risks);
