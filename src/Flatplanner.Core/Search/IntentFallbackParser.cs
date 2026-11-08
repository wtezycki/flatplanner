using System.Globalization;
using System.Text.RegularExpressions;
using Flatplanner.Core.Data;
using Flatplanner.Core.Text;

namespace Flatplanner.Core.Search;

// Only the rules for the ask hard filters (design 16.2 step 1); the rest of the 7.7 table is cut.
public static partial class IntentFallbackParser
{
    private const int MinDistrictWordLength = 4;

    public static SearchIntent Parse(string message, SearchMeta meta)
    {
        var text = TextFolding.FoldKeepingNumberPunctuation(message);
        var plain = TextFolding.Fold(message);
        var (areaMin, areaMax, areaApprox) = Area(text);

        return new SearchIntent
        {
            Cities = Cities(plain),
            Districts = Districts(plain, meta),
            PriceMax = PriceMax(text),
            AreaMin = areaMin,
            AreaMax = areaMax,
            AreaApprox = areaApprox,
            RoomsMin = RoomsMin(text),
        };
    }

    public static AskIntent ParseAsk(string message, SearchMeta meta)
    {
        var parsed = Parse(message, meta);

        return new AskIntent
        {
            Cities = parsed.Cities,
            Districts = parsed.Districts,
            PriceMax = parsed.PriceMax,
            AreaMin = parsed.AreaMin ?? parsed.AreaApprox,
            AreaMax = parsed.AreaMax,
            RoomsMin = parsed.RoomsMin,
        };
    }

    private static string[] Cities(string plain) =>
        Regex.IsMatch(plain, $@"\b(?:{FallbackDictionaries.TricityPattern})\b")
            ? City.All
            : FallbackDictionaries.CityPatterns
                .Where(p => Regex.IsMatch(plain, $@"\b(?:{p.Value})\b"))
                .Select(p => p.Key)
                .ToArray();

    private static string[] Districts(string plain, SearchMeta meta)
    {
        var padded = $" {plain} ";
        var byFullName = meta.Districts.Where(d => padded.Contains($" {TextFolding.Fold(d.Name)} ")).ToArray();
        if (byFullName.Length > 0)
        {
            return byFullName.Select(d => d.Key).ToArray();
        }

        var tokens = plain.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        return meta.Districts
            .Where(d => TextFolding.Fold(d.Name).Split(' ')[0] is { Length: >= MinDistrictWordLength } first && tokens.Contains(first))
            .Select(d => d.Key)
            .ToArray();
    }

    private static (decimal? Min, decimal? Max, decimal? Approx) Area(string text)
    {
        if (AreaRange().Match(text) is { Success: true } range)
        {
            return (Number(range.Groups[1].Value), Number(range.Groups[2].Value), null);
        }

        if (AreaUpperLimit().Match(text) is { Success: true } upper)
        {
            return (null, Number(upper.Groups[1].Value), null);
        }

        return AreaSingle().Match(text) is { Success: true } single
            ? (null, null, Number(single.Groups[1].Value))
            : (null, null, null);
    }

    private static int? PriceMax(string text)
    {
        if (PriceMaxWithUnit().Match(text) is { Success: true } withUnit)
        {
            var multiplier = withUnit.Groups[2].Value == "mln" ? 1_000_000m : 1_000m;
            return (int)(Number(withUnit.Groups[1].Value) * multiplier);
        }

        return PriceMaxInZloty().Match(text) is { Success: true } zloty
            ? int.Parse(zloty.Groups[1].Value.Replace(" ", ""), CultureInfo.InvariantCulture)
            : null;
    }

    private static int? RoomsMin(string text)
    {
        if (RoomsRange().Match(text) is { Success: true } range)
        {
            return int.Parse(range.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        if (Rooms().Match(text) is { Success: true } rooms)
        {
            return int.Parse(rooms.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        return Studio().IsMatch(text) ? 1 : null;
    }

    private static decimal Number(string value) =>
        decimal.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture);

    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(?:-|do)\s*(\d+(?:[.,]\d+)?)\s*(?:m2|m|mkw|metr\w*)\b")]
    private static partial Regex AreaRange();

    [GeneratedRegex(@"(?:do|max|maks\w*|ponizej)\s*(\d+(?:[.,]\d+)?)\s*(?:m2|m|mkw|metr\w*)\b")]
    private static partial Regex AreaUpperLimit();

    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(?:m2|m|mkw|metr\w*)\b")]
    private static partial Regex AreaSingle();

    [GeneratedRegex(@"(?:do|max|maks\w*|ponizej)\s*(\d+(?:[.,]\d+)?)\s*(tys|k|mln)\b")]
    private static partial Regex PriceMaxWithUnit();

    [GeneratedRegex(@"(?:do|max|maks\w*|ponizej)\s*(\d{1,3}(?: \d{3})+|\d{4,})\s*(?:zl|pln)?\b")]
    private static partial Regex PriceMaxInZloty();

    [GeneratedRegex(@"\b(\d)\s*-\s*(\d)\s*pok\w*")]
    private static partial Regex RoomsRange();

    [GeneratedRegex(@"\b(\d)\s*pok\w*")]
    private static partial Regex Rooms();

    [GeneratedRegex(@"kawalerk\w*")]
    private static partial Regex Studio();
}
