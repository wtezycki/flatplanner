using System.Collections.Concurrent;
using Flatplanner.Api.Endpoints;
using Flatplanner.Api.Listings;
using Flatplanner.Core.Data;
using Flatplanner.Core.Llm;
using Flatplanner.Core.Options;
using Flatplanner.Core.Search;
using Flatplanner.Core.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Flatplanner.Api.Chat;

public sealed class AskService(
    FlatplannerDbContext db,
    IntentInterpreter interpreter,
    ListingJudge judge,
    ConcurrentDictionary<string, AskEvaluation> cache,
    IOptions<FlatplannerOptions> options,
    ILogger<AskService> logger)
{
    private static readonly AskNotice AiUnavailable =
        new("ai_unavailable", "AI chwilowo niedostępne — pokazujemy wyniki według podstawowych kryteriów.");

    private static readonly AskNotice JudgeFailed =
        new("judge_failed", "Części ogłoszeń nie udało się sprawdzić — oznaczyliśmy je jako brak danych.");

    public async Task<AskResponse> AskAsync(string message, IReadOnlyCollection<int> excludeNeeds, SearchIntent filters, CancellationToken ct)
    {
        var meta = await SearchMetaLoader.LoadAsync(db, ct);
        var key = TextFolding.Fold(message);
        if (!cache.TryGetValue(key, out var evaluation))
        {
            evaluation = await EvaluateAsync(message, meta, ct);
            if (evaluation.FallbackReason is null && !evaluation.JudgeFailed)
            {
                cache[key] = evaluation;
            }
        }

        var allowed = await NarrowAsync(evaluation, SearchIntentNormalizer.Normalize(filters, meta).Intent, ct);
        return await RespondAsync(evaluation, allowed, excludeNeeds.ToHashSet(), meta, ct);
    }

    private async Task<IReadOnlySet<long>> NarrowAsync(AskEvaluation evaluation, SearchIntent filters, CancellationToken ct)
    {
        var ids = evaluation.Candidates.Select(c => c.Listing.Id).ToList();
        var allowed = await db.Listings.AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .Apply(filters)
            .Select(l => l.Id)
            .ToListAsync(ct);
        return allowed.ToHashSet();
    }

    private async Task<AskEvaluation> EvaluateAsync(string message, SearchMeta meta, CancellationToken ct)
    {
        var (asked, fallbackReason) = await InterpretAsync(message, meta, ct);
        var intent = SearchIntentNormalizer.Normalize(asked.ToSearchIntent(), meta).Intent;

        var matched = await db.Listings.AsNoTracking().Apply(intent).ToListAsync(ct);
        var limited = asked.Needs.Length > 0 && matched.Count > ListingJudge.MaxCandidates;
        var candidates = (limited ? ListingJudge.Newest(matched) : matched).Select(AskCandidate.From).ToList();

        var outcome = asked.Needs.Length > 0 && candidates.Count > 0
            ? await judge.JudgeAsync(asked.Needs, candidates, ct)
            : new JudgeOutcome(new Dictionary<long, IReadOnlyList<JudgedNeed>>(), false);

        var results = candidates.ToDictionary(
            c => c.Listing.Id,
            c => (IReadOnlyList<NeedResult>)Enumerable.Range(0, asked.Needs.Length)
                .Select(i => EvidenceVerifier.Verify(outcome.Judgments.GetValueOrDefault(c.Listing.Id)?.ElementAtOrDefault(i), c))
                .ToList());

        var notices = asked.Clarifications.Select(text => new AskNotice("clarify", text)).ToList();
        if (fallbackReason is not null)
        {
            notices.Add(AiUnavailable);
        }

        if (limited)
        {
            notices.Add(new AskNotice("judge_limited",
                $"Oceniliśmy {candidates.Count} najnowszych z {matched.Count} ogłoszeń. Zawęź miasto, dzielnicę lub cenę, żeby sprawdzić resztę."));
        }

        if (outcome.AnyBatchFailed)
        {
            notices.Add(JudgeFailed);
        }

        return new AskEvaluation(intent, asked.Needs.Select(n => n.Text).ToList(), asked.Declined, notices, fallbackReason, candidates, results, outcome.AnyBatchFailed);
    }

    private async Task<(AskIntent Intent, string? FallbackReason)> InterpretAsync(string message, SearchMeta meta, CancellationToken ct)
    {
        if (options.Value.ChatMode == ChatMode.Fallback)
        {
            return (Fallback(message, meta), FallbackReason.Disabled);
        }

        try
        {
            return (await interpreter.InterpretWithLlmAsync(message, meta, ct), null);
        }
        catch (LlmException ex)
        {
            var reason = FallbackReason.From(ex.Failure);
            logger.LogWarning("Interpretation fell back: {Reason}, prompt {PromptVersion}", reason, PromptVersions.SearchIntent);
            return (Fallback(message, meta), reason);
        }
    }

    private static AskIntent Fallback(string message, SearchMeta meta)
    {
        var parsed = IntentFallbackParser.Parse(message, meta);
        var (areaMin, areaMax) = parsed.AreaApprox is { } approx
            ? SearchDefinitions.AreaApproxRange(approx)
            : (parsed.AreaMin, parsed.AreaMax);

        return new AskIntent
        {
            Cities = parsed.Cities,
            Districts = parsed.Districts,
            PriceMax = parsed.PriceMax,
            AreaMin = areaMin,
            AreaMax = areaMax,
            RoomsMin = parsed.RoomsMin,
        };
    }

    private async Task<AskResponse> RespondAsync(
        AskEvaluation evaluation, IReadOnlySet<long> allowed, IReadOnlySet<int> excluded, SearchMeta meta, CancellationToken ct)
    {
        var filters = InterpretationBuilder.Build(evaluation.Intent, meta, []);
        var summary = string.Join(InterpretationBuilder.Separator, new[] { filters.Summary }.Concat(evaluation.Needs).Where(s => s.Length > 0));

        var rows = evaluation.Candidates
            .Where(c => allowed.Contains(c.Listing.Id))
            .Select(c => new VerdictRow(c.Listing.Id, c.Listing.CreatedAtSource, evaluation.Results[c.Listing.Id].Select(r => r.Verdict).ToList()))
            .ToList();
        var candidates = evaluation.Candidates.ToDictionary(c => c.Listing.Id);
        var arranged = AskGrouping.Arrange(rows, excluded);
        var items = arranged.Select(r => Item(candidates[r.Row.Id], r.Group, evaluation.Results[r.Row.Id])).ToList();

        var relaxations = evaluation.Candidates.Count == 0
            ? await HardFilterRelaxationsAsync(evaluation.Intent, filters.Chips, ct)
            : AskGrouping.Relaxations(evaluation.Needs, rows, excluded);

        return new AskResponse(
            summary,
            evaluation.Needs,
            evaluation.Declined,
            evaluation.Notices,
            items,
            items.Count(i => i.Group == ResultGroup.Unknown),
            relaxations,
            evaluation.FallbackReason is not null,
            evaluation.FallbackReason);
    }

    private static AskItem Item(AskCandidate candidate, string group, IReadOnlyList<NeedResult> needs)
    {
        var listing = candidate.Listing;
        var extras = candidate.Risks.Where(r => r.Kind == RiskKind.ExtraCost && r.AmountPln is not null).Sum(r => r.AmountPln!.Value);

        return new AskItem(
            listing.Id, listing.Title, listing.PricePln, listing.AreaM2, listing.Rooms, listing.Floor, listing.BuildingFloors,
            EnumLabels.CityNames[listing.CitySlug], listing.DistrictName, listing.ImageUrls.FirstOrDefault(), listing.AiSummary,
            listing.LocationApproximate, group, needs, candidate.Risks, extras > 0 ? listing.PricePln + extras : null);
    }

    private async Task<IReadOnlyList<Relaxation>> HardFilterRelaxationsAsync(SearchIntent intent, IReadOnlyList<Chip> chips, CancellationToken ct)
    {
        var relaxations = new List<Relaxation>();
        foreach (var chip in chips)
        {
            var count = await db.Listings.Apply(Without(intent, chip.Key)).CountAsync(ct);
            if (count > 0)
            {
                relaxations.Add(new Relaxation(chip.Label, count, null));
            }
        }

        return relaxations.OrderByDescending(r => r.Count).ToList();
    }

    private static SearchIntent Without(SearchIntent intent, string chipKey) => chipKey switch
    {
        "cities" => intent with { Cities = [], Districts = [] },
        "districts" => intent with { Districts = [] },
        "price" => intent with { PriceMin = null, PriceMax = null },
        "area" => intent with { AreaMin = null, AreaMax = null, AreaApprox = null },
        "rooms" => intent with { RoomsMin = null, RoomsMax = null },
        _ => intent,
    };
}
