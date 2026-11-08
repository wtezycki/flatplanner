using System.Text.Json;

namespace Flatplanner.Ingest.Evaluation;

public static class SearchQueries
{
    public const string Path = "tests/search-queries.json";

    public static IReadOnlyList<SearchQuery> Load(string path) =>
        JsonSerializer.Deserialize<SearchQuery[]>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidDataException($"{path} is empty.");
}
