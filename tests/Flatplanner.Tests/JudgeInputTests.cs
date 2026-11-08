using Flatplanner.Api.Chat;
using Flatplanner.Core.Data;
using Flatplanner.Core.Search;

namespace Flatplanner.Tests;

public sealed class JudgeInputTests
{
    private const string Description = "Mieszkanie jest wyjątkowo ciche.\nBlisko szkoła podstawowa! Własność spółdzielcza. Psy mile widziane?";

    private static readonly NearbyPlace[] Nearby =
    [
        new("Szkoła Podstawowa nr 12", "amenity=school", null, 300),
        new("Szkoła Podstawowa nr 5", "amenity=school", null, 120),
        new("Apteka", "amenity=pharmacy", null, 50),
        new("140: Wiczlino → Gdynia Dworzec", "route=bus", "ref=140", 280),
        new("Grunwaldzka", "highway=primary", null, 90),
    ];

    private static AskNeed Need(string[] places, string[] keywords) =>
        new("potrzeba", places.Select(t => new NeedPlace(t, PlacePreference.Near, null)).ToArray(), keywords);

    [Fact]
    public void Nearby_PlannedTags_NearestPerKindWithOriginalIndex()
    {
        var places = JudgeInput.Nearby(Nearby, [Need(["amenity=school", "highway=primary"], [])]);

        Assert.Equal([1, 4], places.Select(p => p.I));
        Assert.Equal("Szkoła Podstawowa nr 5", places[0].N);
    }

    [Fact]
    public void Nearby_RouteEntry_DropsStopToStopName()
    {
        var route = Assert.Single(JudgeInput.Nearby(Nearby, [Need(["route=bus"], [])]));

        Assert.Null(route.N);
        Assert.Equal("ref=140", route.X);
        Assert.Equal(3, route.I);
    }

    [Fact]
    public void Description_Keywords_SelectsMatchingSentencesVerbatimInOrder()
    {
        var text = JudgeInput.Description(Description, [Need([], ["cich"]), Need([], ["spoldziel", "psy"])]);

        Assert.Equal("Mieszkanie jest wyjątkowo ciche.\nWłasność spółdzielcza.\nPsy mile widziane?", text);
    }

    [Fact]
    public void Description_NoSentenceMatches_Empty()
    {
        Assert.Equal("", JudgeInput.Description(Description, [Need(["amenity=school"], ["winda"])]));
    }

    [Fact]
    public void FullDossier_NeedWithEmptyPlan_KeepsEveryKindAndWholeDescription()
    {
        AskNeed[] needs = [Need(["amenity=school"], []), Need([], [])];

        Assert.Equal([1, 2, 3, 4], JudgeInput.Nearby(Nearby, needs).Select(p => p.I));
        Assert.Equal(Description, JudgeInput.Description(Description, needs));
    }

    [Fact]
    public void HasContext_NoPlannedPlaceAndNoSentence_False()
    {
        Assert.False(JudgeInput.HasContext(Candidate(), [Need(["leisure=dog_park"], ["winda"])]));
    }

    [Fact]
    public void HasContext_PlannedPlaceNearby_True()
    {
        Assert.True(JudgeInput.HasContext(Candidate(), [Need(["leisure=dog_park"], []), Need(["amenity=school"], [])]));
    }

    [Fact]
    public void HasContext_NeedWithEmptyPlan_True()
    {
        Assert.True(JudgeInput.HasContext(Candidate(), [Need(["leisure=dog_park"], ["winda"]), Need([], [])]));
    }

    private static AskCandidate Candidate() => AskCandidate.From(new Listing
    {
        Source = Source.Otodom,
        ExternalId = "1",
        Url = "https://www.otodom.pl/",
        Title = "Mieszkanie",
        DescriptionText = Description,
        SearchText = "",
        CitySlug = City.Gdansk,
        Market = Market.Secondary,
        NearbyJson = System.Text.Json.JsonSerializer.Serialize(Nearby, DossierJson.Options),
    });
}
