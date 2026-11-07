using System.Text.Json;
using Flatplanner.Api.Chat;
using Flatplanner.Core.Data;
using Flatplanner.Core.Search;

namespace Flatplanner.Tests;

public sealed class CandidateRankerTests
{
    private static readonly DateTime Created = new(2026, 11, 1);

    private static AskCandidate Candidate(long id, string description = "Mieszkanie.", DateTime? created = null, params NearbyPlace[] nearby) =>
        AskCandidate.From(new Listing
        {
            Id = id,
            CreatedAtSource = created ?? Created,
            Source = Source.Otodom,
            ExternalId = id.ToString(),
            Url = "https://www.otodom.pl/",
            Title = "Mieszkanie",
            DescriptionText = description,
            SearchText = "",
            CitySlug = City.Gdansk,
            Market = Market.Secondary,
            NearbyJson = JsonSerializer.Serialize(nearby, DossierJson.Options),
        });

    private static NearbyPlace Place(string tag, int distanceM) => new(null, tag, null, distanceM);

    private static AskNeed Need(string tag, string prefer, params string[] keywords) => new("potrzeba", [new NeedPlace(tag, prefer)], keywords);

    private static IEnumerable<long> Order(IReadOnlyList<AskNeed> needs, params AskCandidate[] candidates) =>
        CandidateRanker.Rank(candidates, needs).Select(c => c.Listing.Id);

    [Fact]
    public void Rank_NearPreference_NearerPlaceFirst()
    {
        var far = Candidate(1, nearby: Place("amenity=school", 1200));
        var near = Candidate(2, nearby: Place("amenity=school", 200));

        Assert.Equal([2L, 1L], Order([Need("amenity=school", PlacePreference.Near)], far, near));
    }

    [Fact]
    public void Rank_FarPreference_FartherPlaceFirst()
    {
        var near = Candidate(1, nearby: Place("highway=primary", 90));
        var far = Candidate(2, nearby: Place("highway=primary", 1100));

        Assert.Equal([2L, 1L], Order([Need("highway=primary", PlacePreference.Far)], near, far));
    }

    [Fact]
    public void Rank_KeywordSentence_LiftsListing()
    {
        var silent = Candidate(1);
        var quiet = Candidate(2, "Mieszkanie jest bardzo ciche.");

        Assert.Equal([2L, 1L], Order([Need("highway=primary", PlacePreference.Far, "cich")], silent, quiet));
    }

    [Fact]
    public void Rank_TagMissing_RanksBelowPresentFarPlace()
    {
        var missing = Candidate(1, created: Created.AddDays(1));
        var present = Candidate(2, nearby: Place("highway=primary", 1400));

        Assert.Equal([2L, 1L], Order([Need("highway=primary", PlacePreference.Far)], missing, present));
    }

    [Fact]
    public void Rank_EmptyPlans_NewestFirstThenHigherId()
    {
        var older = Candidate(1, created: Created);
        var newer = Candidate(2, created: Created.AddDays(1));
        var sameDay = Candidate(3, created: Created);

        Assert.Equal([2L, 3L, 1L], Order([new AskNeed("potrzeba", [], [])], older, newer, sameDay));
    }
}
