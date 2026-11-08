using Flatplanner.Core.Search;

namespace Flatplanner.Ingest.Evaluation;

public sealed record QueryResult(
    string Id,
    string[] History,
    string Message,
    AskIntent? Llm,
    IReadOnlyList<string> LlmDifferences,
    AskIntent Fallback,
    IReadOnlyList<string> FallbackDifferences);
