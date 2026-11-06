using System.Text.Json;
using System.Text.Json.Nodes;
using Flatplanner.Api.Chat;
using Flatplanner.Core.Llm;
using Flatplanner.Core.Search;

namespace Flatplanner.Tests;

public sealed class SearchIntentSchemaTests
{
    private static readonly SearchMeta Meta = new(
        ["gdansk", "gdynia"],
        [new SearchMetaDistrict("gdansk:piecki--migowo", "gdansk", "Piecki-Migowo")],
        "gdansk");

    public static TheoryData<string> Schemas => new() { SearchIntentSchema.Build(Meta), ListingJudgeSchema.Build() };

    [Theory]
    [MemberData(nameof(Schemas))]
    public void Build_EveryObject_RequiresAllPropertiesAndDisallowsAdditional(string schemaJson)
    {
        var objects = Objects(JsonNode.Parse(schemaJson)!).ToList();

        Assert.NotEmpty(objects);
        Assert.All(objects, o =>
        {
            Assert.False(o["additionalProperties"]!.GetValue<bool>());
            var properties = o["properties"]!.AsObject().Select(p => p.Key).Order();
            var required = o["required"]!.AsArray().Select(r => r!.GetValue<string>()).Order();
            Assert.Equal(properties, required);
        });
    }

    [Fact]
    public void Build_Fields_EqualAskIntentProperties()
    {
        var schemaFields = JsonNode.Parse(SearchIntentSchema.Build(Meta))!["properties"]!.AsObject().Select(p => p.Key).Order();
        var recordFields = typeof(AskIntent).GetProperties().Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)).Order();

        Assert.Equal(recordFields, schemaFields);
    }

    [Fact]
    public void Deserialize_CamelCaseIntent_FillsNeedsAndDeclined()
    {
        const string json = """
            { "cities": ["gdansk"], "districts": ["gdansk:piecki--migowo"], "priceMax": 700000, "areaMin": null, "areaMax": null,
              "roomsMin": 2, "needs": ["podstawówka w zasięgu spaceru"],
              "declined": [{ "text": "bez obcokrajowców", "reason": "Nie oceniamy okolic po tym, kto w nich mieszka." }],
              "clarifications": [] }
            """;

        var intent = JsonSerializer.Deserialize<AskIntent>(json, LlmJsonClient.LlmJson)!;

        Assert.Equal(["gdansk"], intent.Cities);
        Assert.Equal(700000, intent.PriceMax);
        Assert.Equal(["podstawówka w zasięgu spaceru"], intent.Needs);
        Assert.Equal("bez obcokrajowców", Assert.Single(intent.Declined).Text);
    }

    [Fact]
    public void Deserialize_CamelCaseJudgment_FillsVerdictsAndEvidence()
    {
        const string json = """
            { "listings": [{ "id": 70, "needs": [{ "verdict": "yes", "reason": "Szkoła 88 m.",
              "evidence": [{ "kind": "place", "ref": "3" }] }] }] }
            """;

        var response = JsonSerializer.Deserialize<JudgeResponse>(json, LlmJsonClient.LlmJson)!;

        var need = Assert.Single(Assert.Single(response.Listings).Needs);
        Assert.Equal("yes", need.Verdict);
        Assert.Equal("3", Assert.Single(need.Evidence).Ref);
    }

    [Fact]
    public void Deserialize_UnknownField_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<AskIntent>("""{ "cities": [], "text": "morena" }""", LlmJsonClient.LlmJson));
    }

    [Theory]
    [InlineData("""{ "listings": [{ "id": 70, "needs": [{ "verdict": "yes", "reason": "Szkoła." }] }] }""")]
    [InlineData("""{ "listings": [{ "id": 70, "needs": [{ "verdict": "yes", "reason": null, "evidence": [] }] }] }""")]
    public void Deserialize_JudgmentWithMissingOrNullField_Throws(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JudgeResponse>(json, LlmJsonClient.LlmJson));
    }

    private static IEnumerable<JsonObject> Objects(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            if (obj["type"]?.GetValueKind() == JsonValueKind.String && obj["type"]!.GetValue<string>() == "object")
            {
                yield return obj;
            }

            foreach (var child in obj.Select(p => p.Value).OfType<JsonNode>().SelectMany(Objects))
            {
                yield return child;
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array.OfType<JsonNode>().SelectMany(Objects))
            {
                yield return child;
            }
        }
    }
}
