using System.Text.Json.Nodes;
using Flatplanner.Core.Llm;

namespace Flatplanner.Core.Search;

public static class SearchIntentSchema
{
    public const string Name = "search_intent";

    public static string Build(SearchMeta meta) => StrictSchema.Object(new JsonObject
    {
        ["cities"] = StrictSchema.Array(StrictSchema.Enum(meta.CitySlugs)),
        ["districts"] = StrictSchema.Array(StrictSchema.Enum(meta.Districts.Select(d => d.Key))),
        ["priceMax"] = StrictSchema.Nullable("integer"),
        ["areaMin"] = StrictSchema.Nullable("number"),
        ["areaMax"] = StrictSchema.Nullable("number"),
        ["roomsMin"] = StrictSchema.Nullable("integer"),
        ["needs"] = StrictSchema.Array(StrictSchema.String()),
        ["declined"] = StrictSchema.Array(StrictSchema.Object(new JsonObject
        {
            ["text"] = StrictSchema.String(),
            ["reason"] = StrictSchema.String(),
        })),
        ["clarifications"] = StrictSchema.Array(StrictSchema.String()),
    }).ToJsonString();
}
