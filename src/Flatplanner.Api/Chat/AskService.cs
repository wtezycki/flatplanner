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
    ConcurrentDictionary<string, AskInterpretation> interpretations,
    ConcurrentDictionary<VerdictKey, IReadOnlyList<NeedResult>> verdicts,
    IOptions<FlatplannerOptions> options,
    ILogger<AskService> logger)
{
    private static readonly AskNotice AiUnavailable =
        new("ai_unavailable", "AI chwilowo niedostępne — pokazujemy wyniki według podstawowych kryteriów.");

    private static readonly AskNotice JudgeFailed =
        new("judge_failed", "Części ogłoszeń nie udało się sprawdzić — oznaczyliśmy je jako brak danych.");

    public async Task<AskResponse> AskAsync(
        string message, IReadOnlyCollection<int> excludeNeeds, SearchIntent filters, bool more, CancellationToken ct)
    {
        var meta = await SearchMetaLoader.LoadAsync(db, ct);
        var key = TextFolding.Fold(message);
        if (!interpretations.TryGetValue(key, out var interpretation))
        {
            interpretation = await BuildInterpretationAsync(message, meta, ct);
            if (interpretation.FallbackReason is null)
            {
                interpretations[key] = interpretation;
            }
        }

        var viewFilters = SearchIntentNormalizer.Normalize(filters, meta).Intent;
        var listings = await db.Listings.AsNoTracking()
            .Apply(interpretation.Intent)
            .Apply(viewFilters)
            .ToListAsync(ct);
        var needs = interpretation.Asked.Needs;
        var ranked = CandidateRanker.Rank(listings.Select(AskCandidate.From), needs);

        var shown = needs.Length == 0
            ? new Shown(ranked, ranked.ToDictionary(c => c.Listing.Id, _ => (IReadOnlyList<NeedResult>)[]), false)
            : await JudgeWindowAsync(ranked, needs, more, ct);

        var notices = shown.JudgeFailed ? [.. interpretation.Notices, JudgeFailed] : interpretation.Notices;
        return await RespondAsync(interpretation, viewFilters, notices, shown, ranked.Count - shown.Candidates.Count, excludeNeeds.ToHashSet(), meta, ct);
    }

    private async Task<Shown> JudgeWindowAsync(IReadOnlyList<AskCandidate> ranked, AskNeed[] needs, bool more, CancellationToken ct)
    {
        var needsKey = string.Join('\n', needs.Select(n => TextFolding.Fold(n.Text)));
        var results = new Dictionary<long, IReadOnlyList<NeedResult>>();
        foreach (var candidate in ranked)
        {
            if (verdicts.TryGetValue(new VerdictKey(needsKey, candidate.Listing.Id), out var cached))
            {
                results[candidate.Listing.Id] = cached;
            }
        }

        var ids = ranked.Select(c => c.Listing.Id).ToList();
        var byId = ranked.ToDictionary(c => c.Listing.Id);
        var window = ListingJudge.NextWindow(ids, results.ContainsKey, more).Select(id => byId[id]).ToList();
        var unknown = Enumerable.Repeat(NeedResult.Unknown, needs.Length).ToList();
        void Store(AskCandidate candidate, IReadOnlyList<NeedResult> verdict) =>
            results[candidate.Listing.Id] = verdicts[new VerdictKey(needsKey, candidate.Listing.Id)] = verdict;

        foreach (var candidate in window.Where(c => !JudgeInput.HasContext(c, needs)))
        {
            Store(candidate, unknown);
        }

        var sent = window.Where(c => !results.ContainsKey(c.Listing.Id)).ToList();
        var outcome = sent.Count > 0
            ? await judge.JudgeAsync(needs, sent, ct)
            : new JudgeOutcome(new Dictionary<long, IReadOnlyList<JudgedNeed>>(), false);

        foreach (var candidate in sent)
        {
            if (outcome.Judgments.TryGetValue(candidate.Listing.Id, out var judged))
            {
                Store(candidate, Enumerable.Range(0, needs.Length)
                    .Select(i => EvidenceVerifier.Verify(judged.ElementAtOrDefault(i), candidate))
                    .ToList());
            }
            else
            {
                results[candidate.Listing.Id] = unknown;
            }
        }

        var prefix = ListingJudge.JudgedPrefix(ids, results.ContainsKey);
        return new Shown(ranked.Take(prefix).ToList(), results, outcome.AnyBatchFailed);
    }

    private async Task<AskInterpretation> BuildInterpretationAsync(string message, SearchMeta meta, CancellationToken ct)
    {
        var (asked, fallbackReason) = await InterpretAsync(message, meta, ct);
        var notices = asked.Clarifications.Select(text => new AskNotice("clarify", text)).ToList();
        if (fallbackReason is not null)
        {
            notices.Add(AiUnavailable);
        }

        return new AskInterpretation(asked, SearchIntentNormalizer.Normalize(asked.ToSearchIntent(), meta).Intent, notices, fallbackReason);
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
        AskInterpretation interpretation,
        SearchIntent viewFilters,
        IReadOnlyList<AskNotice> notices,
        Shown shown,
        int remainingCount,
        IReadOnlySet<int> excluded,
        SearchMeta meta,
        CancellationToken ct)
    {
        var filters = InterpretationBuilder.Build(interpretation.Intent, meta, []);
        var needs = interpretation.Asked.Needs.Select(n => n.Text).ToList();
        var summary = string.Join(InterpretationBuilder.Separator, new[] { filters.Summary }.Concat(needs).Where(s => s.Length > 0));

        var rows = shown.Candidates
            .Select(c => new VerdictRow(c.Listing.Id, c.Listing.CreatedAtSource, shown.Results[c.Listing.Id].Select(r => r.Verdict).ToList()))
            .ToList();
        var candidates = shown.Candidates.ToDictionary(c => c.Listing.Id);
        var arranged = AskGrouping.Arrange(rows, excluded);
        var items = arranged.Select(r => Item(candidates[r.Row.Id], r.Group, shown.Results[r.Row.Id])).ToList();

        var relaxations = shown.Candidates.Count == 0
            ? await HardFilterRelaxationsAsync(interpretation.Intent, viewFilters, filters.Chips, ct)
            : AskGrouping.Relaxations(needs, rows, excluded);

        return new AskResponse(
            summary,
            needs,
            interpretation.Asked.Declined,
            notices,
            items,
            items.Count(i => i.Group == ResultGroup.Unknown),
            remainingCount,
            relaxations,
            interpretation.FallbackReason is not null,
            interpretation.FallbackReason);
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

    private async Task<IReadOnlyList<Relaxation>> HardFilterRelaxationsAsync(
        SearchIntent intent, SearchIntent viewFilters, IReadOnlyList<Chip> chips, CancellationToken ct)
    {
        var relaxations = new List<Relaxation>();
        foreach (var chip in chips)
        {
            var count = await db.Listings.Apply(Without(intent, chip.Key)).Apply(viewFilters).CountAsync(ct);
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

    private sealed record Shown(IReadOnlyList<AskCandidate> Candidates, IReadOnlyDictionary<long, IReadOnlyList<NeedResult>> Results, bool JudgeFailed);
}
