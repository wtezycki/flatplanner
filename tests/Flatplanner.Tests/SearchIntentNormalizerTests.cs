using Flatplanner.Core.Data;
using Flatplanner.Core.Search;

namespace Flatplanner.Tests;

public sealed class SearchIntentNormalizerTests
{
    private static readonly SearchMeta Meta = new([City.Gdansk], [], City.Gdansk);

    [Theory]
    [InlineData(100, 5_000_000)]
    [InlineData(30_000_000, 40_000_000)]
    public void Normalize_PriceOutsideOldBounds_IsKeptWithoutWarning(int min, int max)
    {
        var (intent, warnings) = SearchIntentNormalizer.Normalize(new SearchIntent { PriceMin = min, PriceMax = max }, Meta);

        Assert.Equal(min, intent.PriceMin);
        Assert.Equal(max, intent.PriceMax);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Normalize_AreaAbove300_IsKept()
    {
        var (intent, _) = SearchIntentNormalizer.Normalize(new SearchIntent { AreaMax = 450m }, Meta);

        Assert.Equal(450m, intent.AreaMax);
    }

    [Fact]
    public void Normalize_MinAboveMax_IsSwappedSilently()
    {
        var (intent, warnings) = SearchIntentNormalizer.Normalize(new SearchIntent { RoomsMin = 4, RoomsMax = 2 }, Meta);

        Assert.Equal(2, intent.RoomsMin);
        Assert.Equal(4, intent.RoomsMax);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Normalize_UnknownEnumValues_AreRemoved()
    {
        var intent = new SearchIntent { FinishCondition = "luxury", Ownership = Ownership.Full, Heating = "solar" };

        var normalized = SearchIntentNormalizer.Normalize(intent, Meta).Intent;

        Assert.Null(normalized.FinishCondition);
        Assert.Null(normalized.Heating);
        Assert.Equal(Ownership.Full, normalized.Ownership);
    }
}
