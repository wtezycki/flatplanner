using Flatplanner.Core.Data;

namespace Flatplanner.Ingest.Enrichment;

public sealed record Dossier(string Summary, IReadOnlyList<ListingRisk> Risks);
