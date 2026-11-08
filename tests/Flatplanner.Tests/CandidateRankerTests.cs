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

    private static NearbyPlace Place(string tag, int distanceM, string? extra = null) => new(null, tag, extra, distanceM);

    private static NearbyPlace Named(string name, string tag, int distanceM) => new(name, tag, null, distanceM);

    private static AskNeed Need(string tag, string prefer, params string[] keywords) => new("potrzeba", [new NeedPlace(tag, prefer, null)], keywords);

    private static AskNeed NeedWithExtra(string tag, string prefer, string extra) => new("potrzeba", [new NeedPlace(tag, prefer, extra)], []);

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
        var silent = Candidate(2);
        var quiet = Candidate(1, "Mieszkanie jest bardzo ciche.");

        Assert.Equal([1L, 2L], Order([Need("highway=primary", PlacePreference.Far, "cich")], silent, quiet));
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

    [Fact]
    public void Rank_NearPlaceAndKeyword_AddUpAboveNearerPlaceAlone()
    {
        var nearer = Candidate(1, nearby: Place("amenity=school", 50));
        var nearWithSentence = Candidate(2, "Szkoła obok.", nearby: Place("amenity=school", 300));

        Assert.Equal([2L, 1L], Order([Need("amenity=school", PlacePreference.Near, "szkol")], nearer, nearWithSentence));
    }

    [Fact]
    public void Rank_ExtraNamesAnotherValue_EntrySkipped()
    {
        var church = Candidate(1, nearby: Place("amenity=place_of_worship", 100, "religion=christian;denomination=roman_catholic"));
        var synagogue = Candidate(2, nearby: Place("amenity=place_of_worship", 600, "religion=jewish;denomination=orthodox"));

        Assert.Equal([2L, 1L], Order([NeedWithExtra("amenity=place_of_worship", PlacePreference.Near, "religion=jewish")], church, synagogue));
    }

    [Fact]
    public void Rank_EntryWithoutExtraKey_Counts()
    {
        var unknownLevel = Candidate(1, nearby: Place("amenity=school", 100));
        var secondary = Candidate(2, created: Created.AddDays(1), nearby: Place("amenity=school", 100, "isced:level=2"));

        Assert.Equal([1L, 2L], Order([NeedWithExtra("amenity=school", PlacePreference.Near, "isced:level=1")], secondary, unknownLevel));
    }

    [Fact]
    public void Rank_SeveralFarTags_NearestNoiseSourceDecides()
    {
        var tramClose = Candidate(1, nearby: [Place("railway=tram", 90), Place("highway=primary", 1300)]);
        var bothMid = Candidate(2, nearby: [Place("railway=tram", 800), Place("highway=primary", 800)]);
        AskNeed quiet = new("cisza", [new NeedPlace("railway=tram", PlacePreference.Far, null), new NeedPlace("highway=primary", PlacePreference.Far, null)], []);

        Assert.Equal([2L, 1L], Order([quiet], tramClose, bothMid));
    }

    [Fact]
    public void Rank_PlaceNameCarriesKeyword_LiftsListing()
    {
        var plainGym = Candidate(1, nearby: Named("Siłownia Fit", "leisure=sports_centre", 700));
        var karateClub = Candidate(2, nearby: Named("Gdański Klub Kyokushin Karate", "leisure=sports_centre", 800));

        Assert.Equal([2L, 1L], Order([Need("leisure=sports_centre", PlacePreference.Near, "karate")], plainGym, karateClub));
    }

    [Fact]
    public void Rank_KeywordInNameOfAnotherTag_NoBonus()
    {
        var withKarateCafe = Candidate(1, nearby: [Named("Siłownia Fit", "leisure=sports_centre", 100), Named("Kawiarnia Karate", "amenity=cafe", 50)]);
        var gymOnly = Candidate(2, created: Created.AddDays(1), nearby: Named("Siłownia Fit", "leisure=sports_centre", 100));

        Assert.Equal([2L, 1L], Order([Need("leisure=sports_centre", PlacePreference.Near, "karate")], withKarateCafe, gymOnly));
    }

    [Fact]
    public void Rank_OrthodoxSynagogue_NotAnOrthodoxChurch()
    {
        var synagogue = Candidate(1, nearby: Place("amenity=place_of_worship", 100, "religion=jewish;denomination=orthodox"));
        var church = Candidate(2, nearby: Place("amenity=place_of_worship", 700, "religion=christian;denomination=orthodox"));

        Assert.Equal([2L, 1L], Order([NeedWithExtra("amenity=place_of_worship", PlacePreference.Near, "denomination=orthodox")], synagogue, church));
    }
}
