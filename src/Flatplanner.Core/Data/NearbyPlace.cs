using System.Text.Json.Serialization;

namespace Flatplanner.Core.Data;

public sealed record NearbyPlace(
    [property: JsonPropertyName("n")] string? Name,
    [property: JsonPropertyName("t")] string Tag,
    [property: JsonPropertyName("x")] string? Extra,
    [property: JsonPropertyName("d")] int DistanceM);
