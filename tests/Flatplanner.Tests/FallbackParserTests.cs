using Flatplanner.Core.Data;
using Flatplanner.Core.Search;
using Flatplanner.Ingest.Evaluation;

namespace Flatplanner.Tests;

public sealed class FallbackParserTests
{
    private static readonly SearchMeta Meta = new([City.Gdansk, City.Gdynia, City.Sopot], [], City.Gdansk);

    private static readonly IReadOnlyList<SearchQuery> Queries = SearchQueries.Load(Path.Combine(AppContext.BaseDirectory, "search-queries.json"));

    public static TheoryData<string> FallbackQueryIds() => [.. Queries.Where(q => q.FallbackExpected).Select(q => q.Id)];

    [Theory]
    [MemberData(nameof(FallbackQueryIds))]
    public void ParseAsk_FallbackQuery_ProducesTheExpectedHardFields(string id)
    {
        var query = Queries.Single(q => q.Id == id);

        var differences = IntentComparison.Differences(query.Expected, IntentFallbackParser.ParseAsk(query.Message, Meta), Meta, checkModelParts: false);

        Assert.Empty(differences);
    }

    [Theory]
    [InlineData("mieszkanie 40 m", 40, null)]
    [InlineData("mieszkanie ok. 40 m2", 40, null)]
    [InlineData("mieszkanie do 60 m2", null, 60)]
    [InlineData("mieszkanie maksymalnie 60 metrów", null, 60)]
    public void ParseAsk_SingleArea_IsAMinimumUnlessItIsAnUpperLimit(string message, int? areaMin, int? areaMax)
    {
        var intent = IntentFallbackParser.ParseAsk(message, Meta);

        Assert.Equal(areaMin, intent.AreaMin);
        Assert.Equal(areaMax, intent.AreaMax);
    }
}
