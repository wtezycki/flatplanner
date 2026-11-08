using Flatplanner.Api.Chat;

namespace Flatplanner.Tests;

public sealed class PlaceLabelsTests
{
    [Fact]
    public void Of_CityCentre_IgnoresTheCityName()
    {
        Assert.Equal("centrum miasta · 338 m", PlaceLabels.Of("Gdańsk", "place=city", 338));
    }

    [Fact]
    public void Of_NamedPlace_KeepsTheName()
    {
        Assert.Equal("Politechnika Gdańska · 356 m", PlaceLabels.Of("Politechnika Gdańska", "amenity=university", 356));
    }

    [Fact]
    public void Of_UnnamedPlace_UsesTheKindLabel()
    {
        Assert.Equal("park · 93 m", PlaceLabels.Of(null, "leisure=park", 93));
    }
}
