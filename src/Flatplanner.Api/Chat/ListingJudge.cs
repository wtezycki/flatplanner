using System.Text.Json;
using Flatplanner.Core.Data;
using Flatplanner.Core.Llm;

namespace Flatplanner.Api.Chat;

public sealed class ListingJudge(LlmJsonClient llm, string promptsDir, string reasoningEffort, ILogger<ListingJudge> logger)
{
    private const int BatchSize = 15;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private const int Parallelism = 4;
    private const int MaxOutputTokens = 8192;
    private const int DescriptionMaxLength = 8000;

    public async Task<JudgeOutcome> JudgeAsync(IReadOnlyList<string> needs, IReadOnlyList<AskCandidate> candidates, CancellationToken ct)
    {
        var prompt = await PromptFiles.LoadAsync(promptsDir, PromptVersions.ListingJudge, ct);
        using var gate = new SemaphoreSlim(Parallelism);

        var batches = await Task.WhenAll(candidates.Chunk(BatchSize).Select(async batch =>
        {
            await gate.WaitAsync(ct);
            try
            {
                return await JudgeBatchAsync(prompt, needs, batch, ct);
            }
            finally
            {
                gate.Release();
            }
        }));

        var judgments = batches
            .OfType<IReadOnlyList<JudgedListing>>()
            .SelectMany(listings => listings)
            .DistinctBy(l => l.Id)
            .ToDictionary(l => l.Id, l => l.Needs);

        return new JudgeOutcome(judgments, batches.Any(b => b is null));
    }

    private async Task<IReadOnlyList<JudgedListing>?> JudgeBatchAsync(
        string prompt,
        IReadOnlyList<string> needs,
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

    private static string Input(IReadOnlyList<string> needs, AskCandidate[] batch) => JsonSerializer.Serialize(new
    {
        needs,
        listings = batch.Select(c => new
        {
            id = c.Listing.Id,
            fields = c.Fields.ToDictionary(f => f.Name, f => f.Value),
            aiSummary = c.Listing.AiSummary,
            description = c.Listing.DescriptionText.Length > DescriptionMaxLength
                ? c.Listing.DescriptionText[..DescriptionMaxLength]
                : c.Listing.DescriptionText,
            nearby = c.Nearby.Select((p, i) => new { i, n = p.Name, t = p.Tag, x = p.Extra, d = p.DistanceM }),
            locationApproximate = c.Listing.LocationApproximate,
        }),
    }, DossierJson.Options);
}
