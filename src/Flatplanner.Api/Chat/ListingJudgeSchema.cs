using System.Text.Json.Nodes;
using Flatplanner.Core.Llm;

namespace Flatplanner.Api.Chat;

public static class ListingJudgeSchema
{
    public const string Name = "listing_judgments";

    public static string Build() => StrictSchema.Object(new JsonObject
    {
        ["listings"] = StrictSchema.Array(StrictSchema.Object(new JsonObject
        {
            ["id"] = new JsonObject { ["type"] = "integer" },
            ["needs"] = StrictSchema.Array(StrictSchema.Object(new JsonObject
            {
                ["verdict"] = StrictSchema.Enum(Verdict.All),
                ["reason"] = StrictSchema.String(),
                ["evidence"] = StrictSchema.Array(StrictSchema.Object(new JsonObject
                {
                    ["kind"] = StrictSchema.Enum(EvidenceKind.All),
                    ["ref"] = StrictSchema.String(),
                })),
            })),
        })),
    }).ToJsonString();
}
