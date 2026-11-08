using System.Text.Json;

namespace Flatplanner.Ingest.Evaluation;

public static class JudgeCases
{
    public const string Path = "tests/judge-cases.json";

    public static IReadOnlyList<JudgeCase> Load(string path) =>
        JsonSerializer.Deserialize<JudgeCase[]>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidDataException($"{path} is empty.");
}
