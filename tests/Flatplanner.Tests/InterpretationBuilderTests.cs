using Flatplanner.Api.Listings;
using Flatplanner.Core.Data;
using Flatplanner.Core.Search;

namespace Flatplanner.Tests;

public sealed class InterpretationBuilderTests
{
    private static readonly SearchMeta Meta = new([City.Gdansk], [], City.Gdansk);

    private static string Summary(SearchIntent intent) => InterpretationBuilder.Build(intent, Meta, []).Summary;

    [Fact]
    public void Build_BuildYearRange_WritesTheYearsWithoutSeparators()
    {
        Assert.Equal("rok budowy 2000–2010", Summary(new SearchIntent { BuildYearMin = 2000, BuildYearMax = 2010 }));
    }

    [Fact]
    public void Build_PricePerM2AndRent_UseTheirUnits()
    {
        var summary = Summary(new SearchIntent { PricePerM2Max = 12_000, RentMax = 700 });

        Assert.Equal("do 12 000 zł/m² · czynsz do 700 zł", summary);
    }

    [Fact]
    public void Build_SeveralValuesOfOneEnum_AreJoinedInOneChip()
    {
        var chips = InterpretationBuilder.Build(
            new SearchIntent { BuildingMaterials = [BuildingMaterial.Brick, BuildingMaterial.ReinforcedConcrete] }, Meta, []).Chips;

        Assert.Equal("materiał: cegła, żelbet", Assert.Single(chips).Label);
    }

    [Fact]
    public void Build_GarageAndBasement_AddTheirChips()
    {
        Assert.Equal("z garażem · z piwnicą", Summary(new SearchIntent { RequireGarage = true, RequireBasement = true }));
    }
}
