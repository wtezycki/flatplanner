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
}
