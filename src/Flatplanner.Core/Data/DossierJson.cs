using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flatplanner.Core.Data;

public static class DossierJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static IReadOnlyList<T> ParseList<T>(string? json) =>
        json is null ? [] : JsonSerializer.Deserialize<T[]>(json, Options) ?? [];
}
