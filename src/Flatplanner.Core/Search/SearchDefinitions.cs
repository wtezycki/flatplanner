namespace Flatplanner.Core.Search;

public static class SearchDefinitions
{
    public const int PageSize = 20;
    public const int TextMaxLength = 100;
    public const int TextMaxTokens = 5;
    public const int TextMinTokenLength = 2;
    public const int MaxNeeds = 4;
    public const int MaxNeedKeywords = 6;
    public const int NeedKeywordMinLength = 3;

    public const decimal AreaApproxLowerFactor = 0.88m;
    public const decimal AreaApproxUpperFactor = 1.12m;

    public static (decimal Min, decimal Max) AreaApproxRange(decimal approx) =>
        (Math.Floor(approx * AreaApproxLowerFactor), Math.Ceiling(approx * AreaApproxUpperFactor));
}
