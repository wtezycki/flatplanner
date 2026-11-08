using System.Text.Encodings.Web;
using System.Text.Json;
using Flatplanner.Core.Data;
using Flatplanner.Core.Llm;
using Flatplanner.Core.Options;
using Flatplanner.Core.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flatplanner.Ingest.Evaluation;

public sealed class EvalIntentsCommand(FlatplannerDbContext db, IOptions<FlatplannerOptions> options, ILoggerFactory loggerFactory)
{
    private const string ReportDirectory = "data/eval";

    private static readonly JsonSerializerOptions ReportJson = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

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
        var results = new List<QueryResult>();
        Console.WriteLine($"prompt {PromptVersions.SearchIntent}, model {settings.LlmModel}, effort {settings.LlmChatReasoningEffort}");
        Console.WriteLine("id | LLM ok? | fallback ok? | differences");

        foreach (var query in queries)
        {
            var (llm, llmDifferences) = await InterpretAsync(interpreter, query, meta, cancellationToken);
            var fallback = IntentFallbackParser.ParseAsk(query.Message, meta);
            var fallbackDifferences = IntentComparison.Differences(query.Expected, fallback, meta, checkModelParts: false);
            results.Add(new QueryResult(query.Id, query.History, query.Message, llm, llmDifferences, fallback, fallbackDifferences));

            var fallbackAsExpected = (fallbackDifferences.Count == 0) == query.FallbackExpected;
            var differences = llmDifferences.Select(d => $"LLM {d}")
                .Concat(query.FallbackExpected ? fallbackDifferences.Select(d => $"fallback {d}") : []);
            Console.WriteLine($"{query.Id} | {Cell(llmDifferences.Count == 0)} | {Cell(fallbackAsExpected)} | {string.Join("; ", differences)}");
        }

        Console.WriteLine($"Score: {results.Count(r => r.LlmDifferences.Count == 0)}/{queries.Count}");
        Console.WriteLine($"Report: {await WriteReportAsync(results, cancellationToken)}");
    }

    private static async Task<string> WriteReportAsync(IReadOnlyList<QueryResult> results, CancellationToken cancellationToken)
    {
        var path = Path.Combine(ReportDirectory, $"{PromptVersions.SearchIntent}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        Directory.CreateDirectory(ReportDirectory);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(results, ReportJson), cancellationToken);
        return path;
    }

    private static string Cell(bool ok) => ok ? "ok" : "FAIL";

    private static async Task<(AskIntent? Intent, IReadOnlyList<string> Differences)> InterpretAsync(
        IntentInterpreter interpreter, SearchQuery query, SearchMeta meta, CancellationToken cancellationToken)
    {
        try
        {
            var intent = await interpreter.InterpretWithLlmAsync(query.Message, query.History, meta, cancellationToken);
            return (intent, IntentComparison.Differences(query.Expected, intent, meta, checkModelParts: true));
        }
        catch (LlmException ex)
        {
            return (null, [$"call failed: {ex.Failure}"]);
        }
    }
}
