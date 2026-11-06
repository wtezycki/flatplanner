using Flatplanner.Core.Data;
using Flatplanner.Core.Text;

namespace Flatplanner.Tests;

public sealed class AnchorValidatorTests
{
    private const string Description =
        "Mieszkanie 2-pokojowe - ostatnie piętro. Miejsce postojowe w hali garażowej dostępne za 60 000 zł, " +
        "komórka lokatorska za 25 tys. zł. Lokal jest obecnie wynajęty do końca roku.";

    [Fact]
    public void Validate_QuoteWithNbspAndEmDash_Accepted()
    {
        var risk = new ListingRisk(RiskKind.Occupied, "Wynajęte", "Mieszkanie 2-pokojowe — ostatnie piętro.");

        Assert.Null(Validate(risk));
    }

    [Fact]
    public void Validate_Paraphrase_QuoteNotFound()
    {
        var risk = new ListingRisk(RiskKind.Occupied, "Wynajęte", "Mieszkanie jest wynajmowane");

        Assert.Equal(RiskRejectReason.QuoteNotFound, Validate(risk));
    }

    [Theory]
    [InlineData("Miejsce postojowe w hali garażowej dostępne za 60 000 zł", 60000)]
    [InlineData("komórka lokatorska za 25 tys. zł", 25000)]
    public void Validate_ExtraCostAmountInQuote_Accepted(string quote, int amount)
    {
        var risk = new ListingRisk(RiskKind.ExtraCost, "Dodatkowo płatne", quote, amount);

        Assert.Null(Validate(risk));
    }

    [Fact]
    public void Validate_AmountNotInQuote_AmountNotInQuote()
    {
        var risk = new ListingRisk(RiskKind.ExtraCost, "Dodatkowo płatne", "komórka lokatorska za 25 tys. zł", 40000);

        Assert.Equal(RiskRejectReason.AmountNotInQuote, Validate(risk));
    }

    [Fact]
    public void Validate_AmountBelowRange_AmountOutOfRange()
    {
        var description = "Miejsce postojowe w garażu podziemnym za 2 000 zł.";
        var risk = new ListingRisk(RiskKind.ExtraCost, "Dodatkowo płatne", "Miejsce postojowe w garażu podziemnym za 2 000 zł", 2000);

        Assert.Equal(RiskRejectReason.AmountOutOfRange, AnchorValidator.Validate(risk, AnchorText.Normalize(description)));
    }

    [Fact]
    public void Validate_AmountOnNonExtraCostKind_AmountNotAllowed()
    {
        var risk = new ListingRisk(RiskKind.Legal, "Hipoteka", "komórka lokatorska za 25 tys. zł", 25000);

        Assert.Equal(RiskRejectReason.AmountNotAllowed, Validate(risk));
    }

    [Fact]
    public void Validate_UnknownKind_UnknownKind()
    {
        var risk = new ListingRisk("pets", "Zwierzęta", "Lokal jest obecnie wynajęty");

        Assert.Equal(RiskRejectReason.UnknownKind, Validate(risk));
    }

    private static string? Validate(ListingRisk risk) => AnchorValidator.Validate(risk, AnchorText.Normalize(Description));
}
