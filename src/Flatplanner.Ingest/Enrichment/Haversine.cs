namespace Flatplanner.Ingest.Enrichment;

public static class Haversine
{
    private const double EarthRadiusM = 6_371_000;

    public static double DistanceM(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2)
            + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Pow(Math.Sin(dLng / 2), 2);
        return 2 * EarthRadiusM * Math.Asin(Math.Sqrt(a));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
