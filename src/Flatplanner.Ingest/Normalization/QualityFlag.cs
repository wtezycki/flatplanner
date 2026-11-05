namespace Flatplanner.Ingest.Normalization;

public static class QualityFlag
{
    public const string PriceOutlier = "price_outlier";
    public const string RentZeroAsMissing = "rent_zero_as_missing";
    public const string RentOutOfRange = "rent_out_of_range";
    public const string FloorHigher10 = "floor_higher_10";
    public const string FloorGarret = "floor_garret";
    public const string BuildYearOutOfRange = "build_year_out_of_range";
    public const string AreaOutOfRange = "area_out_of_range";
    public const string CoordinatesOutsideBbox = "coordinates_outside_bbox";
    public const string MissingDistrict = "missing_district";
    public const string CreatedAtMissing = "created_at_missing";

    public static string UnknownValue(string column) => $"unknown_value:{column}";
}
