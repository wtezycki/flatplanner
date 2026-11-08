using System.Text.Json;

namespace Flatplanner.Ingest.Evaluation;

public sealed record NeedVerdict(string Verdict, string? Reason, JsonElement Evidence);
