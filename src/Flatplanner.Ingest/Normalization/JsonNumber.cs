using System.Globalization;
using System.Text.Json;

namespace Flatplanner.Ingest.Normalization;

public static class JsonNumber
{
    public static decimal? ReadDecimal(JsonElement? element) => element switch
    {
        { ValueKind: JsonValueKind.Number } number => number.GetDecimal(),
        { ValueKind: JsonValueKind.String } text
            when decimal.TryParse(text.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => null,
    };
}
