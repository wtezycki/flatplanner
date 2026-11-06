namespace Flatplanner.Ingest.Enrichment;

public sealed record DroppedRisk(string ExternalId, string? Kind, string Reason);
