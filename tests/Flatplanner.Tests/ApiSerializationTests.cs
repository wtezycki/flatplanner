using System.Text.Json;
using Flatplanner.Api;
using Flatplanner.Api.Listings;
using Flatplanner.Core.Search;

namespace Flatplanner.Tests;

public sealed class ApiSerializationTests
{
    [Fact]
    public void ListResponse_WithMissingOptionalFields_ContainsNoNull()
    {
        var item = new ListingItem(
            17, "Słoneczne", 799000, 41.86m, 19087, 2, null, false, null,
            "gdansk", "Gdańsk", null, [], "secondary",
            new DateTime(2026, 11, 4, 7, 49, 54, DateTimeKind.Utc), false, false);
        var interpretation = new Interpretation(
            "Gdańsk · 40–80 m²",
            [new Chip("cities", "Gdańsk")],
            [new SearchWarning("priceMax", "Cena pominięta.")]);
        var response = new ListingsResponse([item], 1, 20, 1, 1, "newest", interpretation);

        var json = JsonSerializer.Serialize(response, ApiJson.Options);

        Assert.DoesNotContain(":null", json);
        Assert.DoesNotContain("districtName", json);
        Assert.Contains("\"imageUrls\":[]", json);
    }

    [Fact]
    public void ListResponse_Empty_KeepsArraysAndBooleans()
    {
        var response = new ListingsResponse([], 1, 20, 0, 0, "newest", new Interpretation("", [], []));

        var json = JsonSerializer.Serialize(response, ApiJson.Options);

        Assert.Contains("\"items\":[]", json);
        Assert.Contains("\"chips\":[]", json);
        Assert.Contains("\"warnings\":[]", json);
    }
}
