using System.Text.Json;

namespace Flatplanner.Ingest.Normalization;

internal static class JsonElementExtensions
{
    public static JsonElement? Get(this JsonElement? element, params string[] path)
    {
        var current = element;
        foreach (var name in path)
        {
            if (current is not { ValueKind: JsonValueKind.Object } obj
                || !obj.TryGetProperty(name, out var next)
                || next.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            current = next;
        }

        return current;
    }

    public static string? GetString(this JsonElement? element) =>
        element is { ValueKind: JsonValueKind.String } text ? text.GetString() : null;

    public static JsonElement? First(this JsonElement? element) => element switch
    {
        { ValueKind: JsonValueKind.Array } array => array.GetArrayLength() > 0 ? array[0] : null,
        { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
        _ => element,
    };

    public static IEnumerable<JsonElement> Items(this JsonElement? element) =>
        element is { ValueKind: JsonValueKind.Array } array ? array.EnumerateArray() : [];
}
