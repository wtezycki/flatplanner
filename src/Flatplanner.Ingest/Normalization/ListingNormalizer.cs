using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flatplanner.Core.Data;
using Flatplanner.Core.Text;

namespace Flatplanner.Ingest.Normalization;

public static partial class ListingNormalizer
{
    private const int MaxTitleLength = 300;
    private const int MaxImages = 10;
    private const decimal MinAreaM2 = 10;
    private const decimal MaxAreaM2 = 300;
    private const int MinPricePerM2 = 7_000;
    private const int MaxPricePerM2 = 40_000;
    private const int MinRentPln = 50;
    private const int MaxRentPln = 5_000;
    private const int MinRooms = 1;
    private const int MaxRooms = 10;
    private const int MaxBuildingFloors = 60;
    private const int MinBuildYear = 1800;
    private const int BuildYearMargin = 6;
    private const decimal MinLat = 54.25m;
    private const decimal MaxLat = 54.65m;
    private const decimal MinLng = 18.30m;
    private const decimal MaxLng = 18.85m;

    private static readonly Dictionary<string, string> CityNames = new()
    {
        [City.Gdansk] = "Gdańsk",
        [City.Gdynia] = "Gdynia",
        [City.Sopot] = "Sopot",
    };

    public static NormalizeResult Normalize(JsonElement adElement, DateTime scrapedAt)
    {
        JsonElement? ad = adElement;
        var target = ad.Get("target");
        var flags = new SortedSet<string>(StringComparer.Ordinal);

        var externalId = ad.Get("id")?.ToString();
        if (string.IsNullOrEmpty(externalId))
        {
            return NormalizeResult.Reject(RejectReason.MissingId);
        }

        var url = ad.Get("url").GetString();
        if (string.IsNullOrWhiteSpace(url))
        {
            return NormalizeResult.Reject(RejectReason.MissingUrl);
        }

        var title = ad.Get("title").GetString()?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return NormalizeResult.Reject(RejectReason.MissingTitle);
        }

        if (target.Get("ProperType").GetString() != "mieszkanie" || target.Get("OfferType").GetString() != "sprzedaz")
        {
            return NormalizeResult.Reject(RejectReason.NotFlatSale);
        }

        if (target.Get("hidePrice").GetString() == "1")
        {
            return NormalizeResult.Reject(RejectReason.HiddenPrice);
        }

        var price = ReadInt(target.Get("Price"));
        if (price is not > 0)
        {
            return NormalizeResult.Reject(RejectReason.MissingPrice);
        }

        var area = JsonNumber.ReadDecimal(target.Get("Area"));
        if (area is not > 0)
        {
            return NormalizeResult.Reject(RejectReason.MissingArea);
        }

        var areaM2 = Math.Round(area.Value, 2);
        if (areaM2 is < MinAreaM2 or > MaxAreaM2)
        {
            flags.Add(QualityFlag.AreaOutOfRange);
        }

        var pricePerM2 = (int)Math.Round(price.Value / areaM2, MidpointRounding.AwayFromZero);
        if (pricePerM2 is < MinPricePerM2 or > MaxPricePerM2)
        {
            flags.Add(QualityFlag.PriceOutlier);
        }

        var rooms = ReadInt(target.Get("Rooms_num").First());
        if (rooms is not (>= MinRooms and <= MaxRooms))
        {
            return NormalizeResult.Reject(RejectReason.MissingRooms);
        }

        var market = target.Get("MarketType").GetString()?.ToLowerInvariant();
        if (market is null || !Market.All.Contains(market))
        {
            return NormalizeResult.Reject(RejectReason.MissingMarket);
        }

        var citySlug = target.Get("City").GetString() is { } cityName ? TextFolding.Fold(cityName) : null;
        if (citySlug is null || !City.All.Contains(citySlug))
        {
            return NormalizeResult.Reject(RejectReason.CityOutOfScope);
        }

        var lat = JsonNumber.ReadDecimal(ad.Get("location", "coordinates", "latitude"));
        var lng = JsonNumber.ReadDecimal(ad.Get("location", "coordinates", "longitude"));
        if (lat is null || lng is null)
        {
            return NormalizeResult.Reject(RejectReason.MissingCoordinates);
        }

        if (lat is < MinLat or > MaxLat || lng is < MinLng or > MaxLng)
        {
            flags.Add(QualityFlag.CoordinatesOutsideBbox);
        }

        var (districtName, districtSlug) = ReadDistrict(ad);
        if (districtSlug is null)
        {
            flags.Add(QualityFlag.MissingDistrict);
        }

        var street = ad.Get("location", "address", "street", "name").GetString()?.Trim();
        var description = ad.Get("description").GetString() is { } html ? HtmlText.ToPlain(html) : "";

        return NormalizeResult.Accept(new Listing
        {
            Source = Source.Otodom,
            ExternalId = externalId,
            Url = url,
            Title = title.Length > MaxTitleLength ? title[..MaxTitleLength] : title,
            DescriptionText = description,
            SearchText = TextFolding.Fold(
                $"{title} {description} {districtName} {street} {CityNames[citySlug]}"),
            CitySlug = citySlug,
            DistrictSlug = districtSlug,
            DistrictName = districtName,
            Street = string.IsNullOrEmpty(street) ? null : street,
            Lat = Math.Round(lat.Value, 6),
            Lng = Math.Round(lng.Value, 6),
            LocationApproximate = JsonNumber.ReadDecimal(ad.Get("location", "mapDetails", "radius")) > 0,
            PricePln = price.Value,
            AreaM2 = areaM2,
            PricePerM2 = pricePerM2,
            Rooms = (byte)rooms.Value,
            Floor = ReadFloor(target.Get("Floor_no").First().GetString(), flags),
            BuildingFloors = ReadInt(target.Get("Building_floors_num")) is >= 0 and <= MaxBuildingFloors and var floors
                ? (byte)floors
                : null,
            BuildYear = ReadBuildYear(target.Get("Build_year"), scrapedAt, flags),
            Market = market,
            FinishCondition = ReadEnum(target.Get("Construction_status").First(), FinishCondition.All, "finish_condition", flags),
            Ownership = ReadEnum(target.Get("Building_ownership").First(), Ownership.All, "ownership", flags),
            RentPln = ReadRent(target.Get("Rent"), flags),
            HasLift = HasExtra(target, "lift"),
            HasBalcony = HasExtra(target, "balcony"),
            HasGarage = HasExtra(target, "garage"),
            HasBasement = HasExtra(target, "basement"),
            AdvertiserType = ReadEnum(target.Get("user_type"), AdvertiserType.All, "advertiser_type", flags),
            ImageUrls = ad.Get("images").Items()
                .Select(image => ((JsonElement?)image).Get("medium").GetString())
                .OfType<string>()
                .Where(imageUrl => imageUrl.Length > 0)
                .Take(MaxImages)
                .ToArray(),
            QualityFlags = [.. flags],
            CreatedAtSource = ReadCreatedAt(ad.Get("createdAt").GetString(), scrapedAt, flags),
            ScrapedAt = scrapedAt,
        });
    }

    private static int? ReadInt(JsonElement? element) =>
        JsonNumber.ReadDecimal(element) is { } value && value is >= int.MinValue and <= int.MaxValue
            ? (int)Math.Round(value, MidpointRounding.AwayFromZero)
            : null;

    private static (string? Name, string? Slug) ReadDistrict(JsonElement? ad)
    {
        foreach (var location in ad.Get("location", "reverseGeocoding", "locations").Items())
        {
            JsonElement? candidate = location;
            if (candidate.Get("locationLevel").GetString() != "district")
            {
                continue;
            }

            var name = candidate.Get("name").GetString()?.Trim();
            var slug = candidate.Get("id").GetString()?.Split('/').Last();
            return string.IsNullOrEmpty(name) || string.IsNullOrEmpty(slug) ? (null, null) : (name, slug);
        }

        return (null, null);
    }

    private static sbyte? ReadFloor(string? value, SortedSet<string> flags)
    {
        switch (value)
        {
            case null:
                return null;
            case "ground_floor":
                return 0;
            case "cellar":
                return -1;
            case "floor_higher_10":
                flags.Add(QualityFlag.FloorHigher10);
                return 11;
            case "garret":
                flags.Add(QualityFlag.FloorGarret);
                return null;
        }

        var match = FloorNumber().Match(value);
        if (match.Success)
        {
            return sbyte.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        flags.Add(QualityFlag.UnknownValue("floor"));
        return null;
    }

    private static short? ReadBuildYear(JsonElement? element, DateTime scrapedAt, SortedSet<string> flags)
    {
        var year = ReadInt(element);
        if (year is null)
        {
            return null;
        }

        if (year < MinBuildYear || year > scrapedAt.Year + BuildYearMargin)
        {
            flags.Add(QualityFlag.BuildYearOutOfRange);
            return null;
        }

        return (short)year;
    }

    private static string? ReadEnum(JsonElement? element, string[] allowed, string column, SortedSet<string> flags)
    {
        var value = element.GetString();
        if (value is null)
        {
            return null;
        }

        if (allowed.Contains(value))
        {
            return value;
        }

        flags.Add(QualityFlag.UnknownValue(column));
        return null;
    }

    private static int? ReadRent(JsonElement? element, SortedSet<string> flags)
    {
        var rent = ReadInt(element);
        switch (rent)
        {
            case null:
                return null;
            case <= 0:
                flags.Add(QualityFlag.RentZeroAsMissing);
                return null;
            case < MinRentPln or > MaxRentPln:
                flags.Add(QualityFlag.RentOutOfRange);
                return null;
            default:
                return rent;
        }
    }

    private static bool? HasExtra(JsonElement? target, string extra) =>
        target.Get("Extras_types").Items().Any(item => ((JsonElement?)item).GetString() == extra) ? true : null;

    private static DateTime ReadCreatedAt(string? value, DateTime scrapedAt, SortedSet<string> flags)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        flags.Add(QualityFlag.CreatedAtMissing);
        return scrapedAt;
    }

    [GeneratedRegex(@"^floor_(\d{1,2})$")]
    private static partial Regex FloorNumber();
}
