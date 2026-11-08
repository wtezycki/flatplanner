using System.Text.Json;
using Flatplanner.Core.Data;
using Flatplanner.Core.Llm;
using Flatplanner.Core.Search;

namespace Flatplanner.Api.Chat;

public sealed class ListingJudge(LlmJsonClient llm, string promptsDir, string reasoningEffort, ILogger<ListingJudge> logger)
{
    public const int WindowSize = 20;
    private const int BatchSize = 10;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
    private const int MaxOutputTokens = 8192;

    // Every call judges at most one window after the longest judged prefix of the ranking (design 16.9 step 5).
    public static IReadOnlyList<long> NextWindow(IReadOnlyList<long> ranked, Func<long, bool> isJudged, bool more)
    {
        var prefix = JudgedPrefix(ranked, isJudged);
        return prefix >= WindowSize && !more
            ? []
            : ranked.Skip(prefix).Take(WindowSize).Where(id => !isJudged(id)).ToList();
    }

    public static int JudgedPrefix(IReadOnlyList<long> ranked, Func<long, bool> isJudged) =>
        ranked.TakeWhile(isJudged).Count();

    public async Task<JudgeOutcome> JudgeAsync(IReadOnlyList<AskNeed> needs, IReadOnlyList<AskCandidate> candidates, CancellationToken ct)
    {
        var prompt = await PromptFiles.LoadAsync(promptsDir, PromptVersions.ListingJudge, ct);
        var batches = await Task.WhenAll(candidates.Chunk(BatchSize).Select(batch => JudgeBatchAsync(prompt, needs, batch, ct)));

        var judgments = batches
            .OfType<IReadOnlyList<JudgedListing>>()
            .SelectMany(listings => listings)
            .DistinctBy(l => l.Id)
            .ToDictionary(l => l.Id, l => l.Needs);

        return new JudgeOutcome(judgments, batches.Any(b => b is null));
    }

    private async Task<IReadOnlyList<JudgedListing>?> JudgeBatchAsync(
        string prompt,
        IReadOnlyList<AskNeed> needs,
        AskCandidate[] batch,
        CancellationToken ct)
    {
        try
        {
            var response = await llm.CompleteJsonAsync<JudgeResponse>(
                prompt, Input(needs, batch), ListingJudgeSchema.Name, ListingJudgeSchema.Build(), reasoningEffort, MaxOutputTokens, ct);
            var ids = batch.Select(c => c.Listing.Id).ToHashSet();
            return response.Listings.Where(l => ids.Contains(l.Id)).ToList();
        }
        catch (LlmException ex)
        {
            logger.LogWarning("Judge batch of {Count} listings failed: {Failure}, prompt {PromptVersion}",
                batch.Length, ex.Failure, PromptVersions.ListingJudge);
            return null;
        }
    }

    private static string Input(IReadOnlyList<AskNeed> needs, AskCandidate[] batch) => JsonSerializer.Serialize(new
    {
        listings = batch.Select(c => new
        {
            id = c.Listing.Id,
            fields = c.Fields.ToDictionary(f => f.Name, f => f.Value),
            description = JudgeInput.Description(c.Listing.DescriptionText, needs),
            nearby = JudgeInput.Nearby(c.Nearby, needs),
            locationApproximate = c.Listing.LocationApproximate,
        }),
        needs = needs.Select(n => n.Text),
    }, DossierJson.Options);
}
