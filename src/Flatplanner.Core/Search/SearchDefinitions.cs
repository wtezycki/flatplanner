namespace Flatplanner.Core.Search;

public static class SearchDefinitions
{
    public const int PageSize = 20;
    public const int TextMaxLength = 100;
    public const int TextMaxTokens = 5;
    public const int TextMinTokenLength = 2;

    public const decimal AreaApproxLowerFactor = 0.88m;
    public const decimal AreaApproxUpperFactor = 1.12m;

    public static (decimal Min, decimal Max) AreaApproxRange(decimal approx) =>
        (Math.Floor(approx * AreaApproxLowerFactor), Math.Ceiling(approx * AreaApproxUpperFactor));

    public const int PriceMin = 10_000;
    public const int PriceMax = 20_000_000;
    public const int AreaMin = 10;
    public const int AreaMax = 300;
    public const int RoomsMin = 1;
    public const int RoomsMax = 10;
    public const int FloorMin = -1;
    public const int FloorMax = 40;
}
