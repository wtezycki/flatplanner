using System.Text.Json;
using System.Text.Json.Nodes;
using Flatplanner.Core.Data;
using Flatplanner.Ingest.Normalization;

namespace Flatplanner.Tests;

public sealed class NormalizationTests
{
    private const string Primary = "otodom-ad-primary.json";
    private const string Secondary = "otodom-ad-secondary.json";
    private static readonly DateTime ScrapedAt = new(2026, 11, 5, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Normalize_ZeroRent_RentIsNullWithFlag()
    {
        var listing = Accept(Primary);

        Assert.Null(listing.RentPln);
        Assert.Contains(QualityFlag.RentZeroAsMissing, listing.QualityFlags);
    }

    [Fact]
    public void Normalize_PositiveMapRadius_LocationIsApproximate()
    {
        Assert.True(Accept(Primary).LocationApproximate);
        Assert.False(Accept(Secondary).LocationApproximate);
    }

    [Fact]
    public void Normalize_GroundFloor_FloorIsZero()
    {
        Assert.Equal((sbyte?)0, Accept(Secondary).Floor);
    }

    [Fact]
    public void Normalize_LiftInExtras_HasLiftIsTrue()
    {
        Assert.True(Accept(Secondary).HasLift);
    }

    [Fact]
    public void Normalize_NoLiftInExtras_HasLiftIsNullNeverFalse()
    {
        var ad = Load(Secondary);
        ad["target"]!["Extras_types"] = new JsonArray("garage");

        var listing = Accept(ad);

        Assert.Null(listing.HasLift);
    }

    [Theory]
    [InlineData(Primary, "ujescisko--lostowice")]
    [InlineData(Secondary, "suchanino")]
    public void Normalize_ReverseGeocoding_DistrictSlugIsLastIdSegment(string fixture, string expectedSlug)
    {
        Assert.Equal(expectedSlug, Accept(fixture).DistrictSlug);
    }

    [Theory]
    [InlineData(Primary, 12749)]
    [InlineData(Secondary, 15158)]
    public void Normalize_PriceAndArea_PricePerM2IsRounded(string fixture, int expected)
    {
        Assert.Equal(expected, Accept(fixture).PricePerM2);
    }

    [Fact]
    public void Normalize_HtmlDescription_PlainTextWithoutTagsAndNbsp()
    {
        var text = Accept(Secondary).DescriptionText;

        Assert.DoesNotContain('<', text);
        Assert.DoesNotContain('>', text);
        Assert.DoesNotContain(' ', text);
    }

    [Fact]
    public void Normalize_MissingPrice_RejectedWithMissingPrice()
    {
        var ad = Load(Primary);
        ad["target"]!["Price"] = null;

        var result = Normalize(ad);

        Assert.Null(result.Listing);
        Assert.Equal(RejectReason.MissingPrice, result.RejectReason);
    }

    [Fact]
    public void Normalize_EmphasisInDescription_MarkupKeepsItAndPlainTextDoesNot()
    {
        var ad = Load(Primary);
        ad["description"] = "<p><strong>Cisza:</strong> blisko <em>parku</em> *</p><p><b> </b>Koniec</p>";

        var listing = Accept(ad);

        Assert.Equal("**Cisza:** blisko *parku* \\*\nKoniec", listing.DescriptionMarkup);
        Assert.Equal("Cisza: blisko parku *\nKoniec", listing.DescriptionText);
    }

    [Fact]
    public void Normalize_AdjacentBoldRuns_NotTreatedAsEmpty()
    {
        var ad = Load(Primary);
        ad["description"] = "<p><b>a</b> <b>b</b></p>";

        Assert.Equal("**a** **b**", Accept(ad).DescriptionMarkup);
    }

    [Fact]
    public void Normalize_ContactDataInDescription_MaskedAndPriceUntouched()
    {
        var ad = Load(Primary);
        ad["description"] = "<p>Zadzwoń: tel. 501 234 567 lub napisz na jan@example.com. Cena 799 000 zł.</p>";

        var text = Accept(ad).DescriptionText;

        Assert.Contains(HtmlText.PhoneMask, text);
        Assert.Contains(HtmlText.EmailMask, text);
        Assert.DoesNotContain("501", text);
        Assert.DoesNotContain("jan@example.com", text);
        Assert.Contains("799 000 zł", text);
    }

    private static JsonNode Load(string fixture) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture)))!;

    private static NormalizeResult Normalize(JsonNode ad)
    {
        using var document = JsonDocument.Parse(ad.ToJsonString());
        return ListingNormalizer.Normalize(document.RootElement, ScrapedAt);
    }

    private static Listing Accept(JsonNode ad) =>
        Normalize(ad).Listing ?? throw new InvalidOperationException("Fixture was rejected");

    private static Listing Accept(string fixture) => Accept(Load(fixture));
}
