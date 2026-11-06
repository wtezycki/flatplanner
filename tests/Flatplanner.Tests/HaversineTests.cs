using Flatplanner.Ingest.Enrichment;

namespace Flatplanner.Tests;

public sealed class HaversineTests
{
    [Fact]
    public void DistanceM_GdanskToGdynia_AboutTwentyKilometres()
    {
        var distance = Haversine.DistanceM(54.3520, 18.6466, 54.5189, 18.5305);

        Assert.InRange(distance, 19_900, 20_100);
    }

    [Fact]
    public void DistanceM_SamePoint_Zero()
    {
        Assert.Equal(0, Haversine.DistanceM(54.35, 18.65, 54.35, 18.65), precision: 6);
    }
}
