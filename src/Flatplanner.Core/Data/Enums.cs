namespace Flatplanner.Core.Data;

public static class Source
{
    public const string Otodom = "otodom";
}

public static class City
{
    public const string Gdansk = "gdansk";
    public const string Gdynia = "gdynia";
    public const string Sopot = "sopot";

    public static readonly string[] All = [Gdansk, Gdynia, Sopot];
}

public static class Market
{
    public const string Primary = "primary";
    public const string Secondary = "secondary";

    public static readonly string[] All = [Primary, Secondary];
}

public static class AdvertiserType
{
    public const string Agency = "agency";
    public const string Private = "private";

    public static readonly string[] All = [Agency, Private];
}

public static class FinishCondition
{
    public const string ReadyToUse = "ready_to_use";
    public const string ToCompletion = "to_completion";
    public const string ToRenovation = "to_renovation";

    public static readonly string[] All = [ReadyToUse, ToCompletion, ToRenovation];
}

public static class Ownership
{
    public const string Full = "full_ownership";
    public const string Limited = "limited_ownership";

    public static readonly string[] All = [Full, Limited];
}

public static class FactsStatus
{
    public const string Pending = "pending";
    public const string Done = "done";
    public const string Skipped = "skipped";
    public const string Failed = "failed";

    public static readonly string[] All = [Pending, Done, Skipped, Failed];
}

public static class FactKey
{
    public const string Parking = "parking";
    public const string Storage = "storage";

    public static readonly string[] All = [Parking, Storage];
}

public static class FactStatus
{
    public const string Included = "included";
    public const string ExtraCost = "extra_cost";
    public const string NotAvailable = "not_available";

    public static readonly string[] All = [Included, ExtraCost, NotAvailable];
}

public static class NormalizeStatus
{
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
}

public static class ValidationStatus
{
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
}

public static class SortOrder
{
    public const string Newest = "newest";
    public const string PriceAsc = "price_asc";
    public const string PriceDesc = "price_desc";
    public const string PricePerM2Asc = "price_per_m2_asc";
    public const string Relevance = "relevance";

    public static readonly string[] All = [Newest, PriceAsc, PriceDesc, PricePerM2Asc, Relevance];
}

public static class Provenance
{
    public const string Listing = "listing";
    public const string Description = "description";
    public const string Computed = "computed";
    public const string Missing = "missing";

    public static readonly string[] All = [Listing, Description, Computed, Missing];
}

public static class ChatMode
{
    public const string Llm = "llm";
    public const string Fallback = "fallback";
}
