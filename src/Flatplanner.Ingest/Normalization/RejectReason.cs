namespace Flatplanner.Ingest.Normalization;

public static class RejectReason
{
    public const string MissingId = "missing_id";
    public const string MissingUrl = "missing_url";
    public const string MissingTitle = "missing_title";
    public const string NotFlatSale = "not_flat_sale";
    public const string HiddenPrice = "hidden_price";
    public const string MissingPrice = "missing_price";
    public const string MissingArea = "missing_area";
    public const string MissingRooms = "missing_rooms";
    public const string MissingMarket = "missing_market";
    public const string CityOutOfScope = "city_out_of_scope";
    public const string MissingCoordinates = "missing_coordinates";
}
