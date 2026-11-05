using System.Net;
using System.Text.RegularExpressions;

namespace Flatplanner.Ingest.Normalization;

public static partial class HtmlText
{
    public const string EmailMask = "[e-mail ukryty]";
    public const string PhoneMask = "[telefon ukryty]";

    public static string ToPlain(string html)
    {
        var text = LineBreakTag().Replace(html, "\n");
        text = ListItemOpen().Replace(text, "\n- ");
        text = AnyTag().Replace(text, "");
        text = WebUtility.HtmlDecode(text);
        text = text.Replace(' ', ' ').Replace(' ', ' ').Replace(' ', ' ');
        text = string.Join('\n', text.Split('\n').Select(line => Spaces().Replace(line, " ").Trim()));
        text = ManyNewLines().Replace(text, "\n\n").Trim();
        text = Email().Replace(text, EmailMask);
        text = PhoneMobile().Replace(text, PhoneMask);
        return PhoneLandline().Replace(text, PhoneMask);
    }

    [GeneratedRegex(@"<br\s*/?>|</p>|</div>|</h[1-6]>|</li>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakTag();

    [GeneratedRegex(@"<li[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ListItemOpen();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ManyNewLines();

    [GeneratedRegex(@"[\w.+-]+@[\w-]+(\.[\w-]+)+")]
    private static partial Regex Email();

    [GeneratedRegex(@"(?:\+48[\s-]?)?\b\d{3}[\s-]?\d{3}[\s-]?\d{3}\b")]
    private static partial Regex PhoneMobile();

    [GeneratedRegex(@"\b\d{2}[\s-]?\d{3}[\s-]?\d{2}[\s-]?\d{2}\b")]
    private static partial Regex PhoneLandline();
}
