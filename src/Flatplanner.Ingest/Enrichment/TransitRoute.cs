namespace Flatplanner.Ingest.Enrichment;

public sealed record TransitRoute(string Tag, string Ref, string Label, (double Lat, double Lng)[] Stops);
