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

    public static double DistanceToSegmentM(double lat, double lng, (double Lat, double Lng) start, (double Lat, double Lng) end)
    {
        var lngScale = Math.Cos(ToRadians(lat));
        var (ax, ay) = ((start.Lng - lng) * lngScale, start.Lat - lat);
        var (dx, dy) = ((end.Lng - start.Lng) * lngScale, end.Lat - start.Lat);
        var lengthSquared = dx * dx + dy * dy;
        var t = lengthSquared == 0 ? 0 : Math.Clamp(-(ax * dx + ay * dy) / lengthSquared, 0, 1);
        return DistanceM(lat, lng, start.Lat + t * (end.Lat - start.Lat), start.Lng + t * (end.Lng - start.Lng));
    }

    public static double DistanceToPolylineM(double lat, double lng, IReadOnlyList<(double Lat, double Lng)> points) =>
        points.Count == 1
            ? DistanceM(lat, lng, points[0].Lat, points[0].Lng)
            : Enumerable.Range(0, points.Count - 1).Min(i => DistanceToSegmentM(lat, lng, points[i], points[i + 1]));

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
