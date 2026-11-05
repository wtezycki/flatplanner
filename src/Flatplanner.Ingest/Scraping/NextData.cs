using System.Text.Json;
using System.Text.RegularExpressions;

namespace Flatplanner.Ingest.Scraping;

public static partial class NextData
{
    public static JsonDocument? Parse(string html)
    {
        var match = Script().Match(html);
        return match.Success ? JsonDocument.Parse(match.Groups[1].Value) : null;
    }

    [GeneratedRegex(@"<script id=""__NEXT_DATA__"" type=""application/json""[^>]*>(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex Script();
}
