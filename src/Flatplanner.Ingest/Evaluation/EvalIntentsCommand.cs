using Flatplanner.Core.Data;
using Flatplanner.Core.Llm;
using Flatplanner.Core.Options;
using Flatplanner.Core.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flatplanner.Ingest.Evaluation;

public sealed class EvalIntentsCommand(FlatplannerDbContext db, IOptions<FlatplannerOptions> options, ILoggerFactory loggerFactory)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.OpenAiApiKey))
        {
            throw new InvalidOperationException("OPENAI_API_KEY is required by eval-intents.");
        }

        var meta = await SearchMetaLoader.LoadAsync(db, cancellationToken);
        var interpreter = new IntentInterpreter(
            new LlmJsonClient(settings.LlmModel, settings.OpenAiApiKey, TimeSpan.FromSeconds(settings.ChatTimeoutSeconds), maxRetries: 1,
                loggerFactory.CreateLogger<LlmJsonClient>()),
            settings.PromptsDir,
            settings.LlmChatReasoningEffort,
            PlaceLabels.AllTags);

        var queries = SearchQueries.Load(SearchQueries.Path);
        var matching = 0;
        Console.WriteLine($"prompt {PromptVersions.SearchIntent}, model {settings.LlmModel}, effort {settings.LlmChatReasoningEffort}");
        Console.WriteLine("id | LLM ok? | fallback ok? | differences");

        foreach (var query in queries)
        {
            var llmDifferences = await LlmDifferencesAsync(interpreter, query, meta, cancellationToken);
            var fallbackDifferences = IntentComparison.Differences(query.Expected, IntentFallbackParser.ParseAsk(query.Message, meta), meta, checkModelParts: false);
            var fallbackAsExpected = (fallbackDifferences.Count == 0) == query.FallbackExpected;
            matching += llmDifferences.Count == 0 ? 1 : 0;

            var differences = llmDifferences.Select(d => $"LLM {d}")
                .Concat(query.FallbackExpected ? fallbackDifferences.Select(d => $"fallback {d}") : []);
            Console.WriteLine($"{query.Id} | {Cell(llmDifferences.Count == 0)} | {Cell(fallbackAsExpected)} | {string.Join("; ", differences)}");
        }

        Console.WriteLine($"Score: {matching}/{queries.Count}");
    }

    private static string Cell(bool ok) => ok ? "ok" : "FAIL";

    private static async Task<IReadOnlyList<string>> LlmDifferencesAsync(
        IntentInterpreter interpreter, SearchQuery query, SearchMeta meta, CancellationToken cancellationToken)
    {
        try
        {
            var intent = await interpreter.InterpretWithLlmAsync(query.Message, query.History, meta, cancellationToken);
            return IntentComparison.Differences(query.Expected, intent, meta, checkModelParts: true);
        }
        catch (LlmException ex)
        {
            return [$"call failed: {ex.Failure}"];
        }
    }
}
