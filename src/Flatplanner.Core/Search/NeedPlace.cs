using Flatplanner.Core.Data;

namespace Flatplanner.Core.Search;

public sealed record NeedPlace(string Tag, string Prefer, string? Extra)
{
    private const string ReligionKey = "religion=";
    private const string DenominationKey = "denomination=";
    private const string ChristianReligion = "religion=christian";

    public bool Accepts(NearbyPlace place) => place.Tag == Tag && ExtraMatches(place.Extra);

    // An entry without the key counts: most OSM schools carry no `isced:level` (design 16.9).
    private bool ExtraMatches(string? entryExtra)
    {
        if (Extra is null || entryExtra is null)
        {
            return true;
        }

        var key = Extra.Split('=')[0] + "=";
        var values = entryExtra.Split(';');
        // OSM gives Orthodox Judaism `denomination=orthodox` too, so a denomination named by a need is a Christian one.
        if (key == DenominationKey && values.Any(v => v.StartsWith(ReligionKey, StringComparison.Ordinal) && v != ChristianReligion))
        {
            return false;
        }

        return !values.Any(v => v.StartsWith(key, StringComparison.Ordinal)) || values.Contains(Extra);
    }
}
