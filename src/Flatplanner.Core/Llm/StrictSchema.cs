using System.Text.Json.Nodes;

namespace Flatplanner.Core.Llm;

public static class StrictSchema
{
    public static JsonObject Object(JsonObject properties) => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["required"] = new JsonArray(properties.Select(p => JsonValue.Create(p.Key)).ToArray<JsonNode?>()),
        ["properties"] = properties,
    };

    public static JsonObject Array(JsonNode items) => new() { ["type"] = "array", ["items"] = items };

    public static JsonObject Boolean() => new() { ["type"] = "boolean" };

    public static JsonObject String() => new() { ["type"] = "string" };

    public static JsonObject Enum(IEnumerable<string> values) => new()
    {
        ["type"] = "string",
        ["enum"] = new JsonArray(values.Select(v => JsonValue.Create(v)).ToArray<JsonNode?>()),
    };

    public static JsonObject Nullable(string type) => new() { ["type"] = new JsonArray(type, "null") };
}
