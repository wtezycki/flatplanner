using Flatplanner.Api.Listings;
using Flatplanner.Core.Data;

namespace Flatplanner.Tests;

public sealed class CardSignalsTests
{
    [Theory]
    [InlineData(new[] { 10_000, 12_000, 14_000 }, 12_000)]
    [InlineData(new[] { 10_000, 12_050, 12_100, 20_000 }, 12_100)]
    public void Median_RoundsToHundredZloty(int[] values, int expected)
    {
        Assert.Equal(expected, CardSignals.Median(values));
    }

    [Theory]
    [InlineData(10_800, 12_000, 10)]
    [InlineData(11_000, 12_000, null)]
    [InlineData(13_000, 12_000, null)]
    [InlineData(9_000, null, null)]
    public void BelowMedianPercent_OnlyFromThreshold(int pricePerM2, int? median, int? expected)
    {
        Assert.Equal(expected, CardSignals.BelowMedianPercent(pricePerM2, median));
    }

    [Fact]
    public void RiskKinds_DuplicatesAndOrder_AreDistinctInCanonicalOrder()
    {
        const string json = """
            [{"kind":"legal","reason":"a","quote":"a"},{"kind":"extra_cost","reason":"b","quote":"b"},{"kind":"legal","reason":"c","quote":"c"}]
            """;

        Assert.Equal([RiskKind.ExtraCost, RiskKind.Legal], CardSignals.RiskKinds(json));
    }

    [Fact]
    public void NearestStopAndPark_PickTheClosestOfEachKind_LabelledWithDistance()
    {
        const string json = """
            [{"t":"highway=bus_stop","d":300},{"t":"railway=tram_stop","d":120},{"t":"leisure=park","d":450},{"t":"leisure=park","d":900}]
            """;

        Assert.Equal("przystanek tramwajowy · 120 m", CardSignals.NearestStop(json, false));
        Assert.Equal("park · 450 m", CardSignals.NearestPark(json, false));
    }

    [Fact]
    public void NearestStop_ApproximateLocationOrTooFar_ReturnsNull()
    {
        const string far = """[{"t":"railway=tram_stop","d":1200}]""";
        const string near = """[{"t":"railway=tram_stop","d":100}]""";

        Assert.Null(CardSignals.NearestStop(far, false));
        Assert.Null(CardSignals.NearestStop(near, true));
        Assert.Null(CardSignals.NearestStop(null, false));
    }
}
