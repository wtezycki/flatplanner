using Flatplanner.Api.Chat;
using Flatplanner.Core.Data;

namespace Flatplanner.Tests;

public sealed class EvidenceVerifierTests
{
    private const string Description = "Szkoła podstawowa - 2 minuty pieszo. Na mieszkaniu jest hipoteka banku.";
    private const string NearbyJson = """[{ "n": "Szkoła Podstawowa nr 12", "t": "amenity=school", "d": 88 }]""";

    [Fact]
    public void Verify_QuoteInDescription_KeepsVerdictWithQuoteLabel()
    {
        var result = Verify(Verdict.Yes, new JudgedEvidence(EvidenceKind.Quote, "Szkoła podstawowa — 2 minuty pieszo"));

        Assert.Equal(Verdict.Yes, result.Verdict);
        Assert.Equal("Szkoła podstawowa — 2 minuty pieszo", Assert.Single(result.Evidence).Label);
    }

    [Fact]
    public void Verify_QuoteNotInDescription_Unknown()
    {
        var result = Verify(Verdict.Yes, new JudgedEvidence(EvidenceKind.Quote, "szkoła tuż obok"));

        Assert.Equal(NeedResult.Unknown, result);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("-1")]
    [InlineData("pierwsza")]
    public void Verify_PlaceIndexOutOfRange_Unknown(string reference)
    {
        var result = Verify(Verdict.No, new JudgedEvidence(EvidenceKind.Place, reference));

        Assert.Equal(NeedResult.Unknown, result);
    }

    [Fact]
    public void Verify_PlaceIndex_LabelFromDossier()
    {
        var result = Verify(Verdict.Yes, new JudgedEvidence(EvidenceKind.Place, "0"));

        Assert.Equal("Szkoła Podstawowa nr 12 · 88 m", Assert.Single(result.Evidence).Label);
    }

    [Fact]
    public void Verify_PlaceOnlyYesWithApproximateLocation_Partly()
    {
        var result = Verify(Verdict.Yes, new JudgedEvidence(EvidenceKind.Place, "0"), locationApproximate: true);

        Assert.Equal(Verdict.Partly, result.Verdict);
    }

    [Fact]
    public void Verify_QuoteYesWithApproximateLocation_StaysYes()
    {
        var result = Verify(Verdict.Yes, new JudgedEvidence(EvidenceKind.Quote, "2 minuty pieszo"), locationApproximate: true);

        Assert.Equal(Verdict.Yes, result.Verdict);
    }

    [Fact]
    public void Verify_FieldWithValue_LabelFromField()
    {
        var result = Verify(Verdict.No, new JudgedEvidence(EvidenceKind.Field, "ownership"));

        Assert.Equal("Forma własności: pełna własność", Assert.Single(result.Evidence).Label);
    }

    [Theory]
    [InlineData("rentPln")]
    [InlineData("seller")]
    public void Verify_FieldWithoutValueOrUnknown_Unknown(string field)
    {
        var result = Verify(Verdict.No, new JudgedEvidence(EvidenceKind.Field, field));

        Assert.Equal(NeedResult.Unknown, result);
    }

    private static NeedResult Verify(string verdict, JudgedEvidence evidence, bool locationApproximate = false)
    {
        var listing = new Listing
        {
            Source = Source.Otodom,
            ExternalId = "68604420",
            Url = "https://www.otodom.pl/",
            Title = "Mieszkanie",
            DescriptionText = Description,
            SearchText = "",
            CitySlug = City.Gdansk,
            Market = Market.Secondary,
            Ownership = Ownership.Full,
            LocationApproximate = locationApproximate,
            NearbyJson = NearbyJson,
        };

        return EvidenceVerifier.Verify(new JudgedNeed(verdict, "Powód.", [evidence]), AskCandidate.From(listing));
    }
}
