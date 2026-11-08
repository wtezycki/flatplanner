using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using Flatplanner.Core.Llm;
using Flatplanner.Core.Text;

namespace Flatplanner.Ingest.Evaluation;

public sealed class EvalJudgeCommand : IDisposable
{
    private const string ApiUrl = "http://localhost:5080";
    private const string ReportDirectory = "data/eval";
    private const int MaxWindows = 5;
    private const string EventPrefix = "event: ";
    private const string DataPrefix = "data: ";
    private static readonly TimeSpan DefaultRetryAfter = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly HttpClient http = new() { BaseAddress = new Uri(ApiUrl), Timeout = TimeSpan.FromMinutes(2) };

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cases = JudgeCases.Load(JudgeCases.Path);
        var results = new List<JudgeResult>();
        var verdictCount = 0;
        var unknownCount = 0;
        Console.WriteLine($"prompts {PromptVersions.SearchIntent}, {PromptVersions.ListingJudge}; API {ApiUrl}");
        Console.WriteLine("case | listing | need | expected | got | ok?");

        foreach (var judgeCase in cases)
        {
            var answer = await AskUntilJudgedAsync(judgeCase, cancellationToken);
            var verdicts = answer.Items.SelectMany(i => i.Needs).Select(n => n.Verdict).ToList();
            verdictCount += verdicts.Count;
            unknownCount += verdicts.Count(v => v == "unknown");

            foreach (var expectation in judgeCase.Expectations)
            {
                var result = Evaluate(judgeCase.Id, expectation, answer);
                results.Add(result);
                Console.WriteLine($"{result.CaseId} | {result.Listing} | {result.Need} | {string.Join('/', result.ExpectedVerdicts)} | {result.Actual?.Verdict} | {(result.Difference is null ? "ok" : $"FAIL {result.Difference}")}");
            }
        }

        Console.WriteLine($"Score: {results.Count(r => r.Difference is null)}/{results.Count}");
        Console.WriteLine($"Unknown share: {unknownCount}/{verdictCount} verdicts in the shown windows");
        Console.WriteLine($"Report: {await WriteReportAsync(results, cancellationToken)}");
    }

    public void Dispose() => http.Dispose();

    private async Task<AskAnswer> AskUntilJudgedAsync(JudgeCase judgeCase, CancellationToken cancellationToken)
    {
        var wanted = judgeCase.Expectations.Select(e => e.Listing).Distinct().ToList();
        var answer = await AskAsync(judgeCase.Message, more: false, cancellationToken);
        for (var window = 1; window < MaxWindows && answer.RemainingCount > 0 && !wanted.All(id => answer.Items.Any(i => i.Listing.Id == id)); window++)
        {
            answer = await AskAsync(judgeCase.Message, more: true, cancellationToken);
        }

        return answer;
    }

    private async Task<AskAnswer> AskAsync(string message, bool more, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/search/ask/stream")
                {
                    Content = JsonContent.Create(new { message, more }, options: Json),
                };
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(response.Headers.RetryAfter?.Delta ?? DefaultRetryAfter, cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return await ReadResultsAsync(response, cancellationToken);
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"The API at {ApiUrl} did not answer; start it with CHAT_MODE=llm.", ex);
        }
    }

    private static async Task<AskAnswer> ReadResultsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
        var eventName = "";
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith(EventPrefix, StringComparison.Ordinal))
            {
                eventName = line[EventPrefix.Length..];
            }
            else if (line.StartsWith(DataPrefix, StringComparison.Ordinal))
            {
                if (eventName == "error")
                {
                    throw new InvalidDataException("The ask stream reported an error.");
                }

                if (eventName == "results")
                {
                    return JsonSerializer.Deserialize<AskAnswer>(line[DataPrefix.Length..], Json)
                        ?? throw new InvalidDataException("The results event is empty.");
                }
            }
        }

        throw new InvalidDataException("The ask stream ended without results.");
    }

    private static JudgeResult Evaluate(string caseId, JudgeExpectation expectation, AskAnswer answer)
    {
        JudgeResult Fail(string difference, string? need = null, NeedVerdict? actual = null) =>
            new(caseId, expectation.Listing, expectation.Verdicts, need, actual, difference);

        if (answer.UsedFallback)
        {
            return Fail($"the API answered with the fallback ({answer.FallbackReason}), nothing was judged");
        }

        var needIndex = answer.Needs.ToList().FindIndex(text => Mentions(text, expectation.Need));
        if (needIndex < 0)
        {
            return Fail($"no need mentioning [{string.Join(", ", expectation.Need)}]; needs: [{string.Join("; ", answer.Needs)}]");
        }

        var needText = answer.Needs[needIndex];
        var item = answer.Items.FirstOrDefault(i => i.Listing.Id == expectation.Listing);
        if (item is null)
        {
            return Fail($"not in the results (hidden or outside the {MaxWindows} windows read)", needText);
        }

        var actual = item.Needs[needIndex];
        return expectation.Verdicts.Contains(actual.Verdict)
            ? new JudgeResult(caseId, expectation.Listing, expectation.Verdicts, needText, actual, null)
            : Fail("verdict differs", needText, actual);
    }

    private static bool Mentions(string text, string[] fragments)
    {
        var folded = TextFolding.Fold(text);
        return fragments.Any(fragment => folded.Contains(TextFolding.Fold(fragment), StringComparison.Ordinal));
    }

    private static async Task<string> WriteReportAsync(IReadOnlyList<JudgeResult> results, CancellationToken cancellationToken)
    {
        var path = Path.Combine(ReportDirectory, $"judge-{PromptVersions.ListingJudge}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        Directory.CreateDirectory(ReportDirectory);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(results, Json), cancellationToken);
        return path;
    }
}
