using System.Net;
using System.Text.RegularExpressions;

namespace Flatplanner.Ingest.Normalization;

public static partial class HtmlText
{
    public const string EmailMask = "[e-mail ukryty]";
    public const string PhoneMask = "[telefon ukryty]";

    private const string BoldOpen = "\uE001";
    private const string ItalicOpen = "\uE002";
    private const string BoldClose = "\uE003";
    private const string ItalicClose = "\uE004";

    public static string ToPlain(string html) => ToText(html, keepEmphasis: false);

    public static string? ToMarkup(string html)
    {
        var markup = ToText(html, keepEmphasis: true);
        return WithoutWhitespace(StripEmphasis(markup)) == WithoutWhitespace(ToPlain(html)) ? markup : null;
    }

    private static string ToText(string html, bool keepEmphasis)
    {
        var text = keepEmphasis ? MarkEmphasis(html) : html;
        text = LineBreakTag().Replace(text, "\n");
        text = ListItemOpen().Replace(text, "\n- ");
        text = AnyTag().Replace(text, "");
        text = WebUtility.HtmlDecode(text);
        text = text.Replace(' ', ' ').Replace(' ', ' ').Replace(' ', ' ');
        text = string.Join('\n', text.Split('\n').Select(line => Spaces().Replace(line, " ").Trim()));
        text = ManyNewLines().Replace(text, "\n\n").Trim();
        text = Email().Replace(text, EmailMask);
        text = PhoneMobile().Replace(text, PhoneMask);
        text = PhoneLandline().Replace(text, PhoneMask);
        return keepEmphasis ? RenderEmphasis(text) : text;
    }

    private static string StripEmphasis(string markup) =>
        EmphasisMarker().Replace(markup, match => match.Groups[1].Value);

    private static string WithoutWhitespace(string text) => Whitespace().Replace(text, "");

    private static string MarkEmphasis(string html)
    {
        var text = BoldOpenTag().Replace(html, BoldOpen);
        text = BoldCloseTag().Replace(text, BoldClose);
        text = ItalicOpenTag().Replace(text, ItalicOpen);
        return ItalicCloseTag().Replace(text, ItalicClose);
    }

    private static string RenderEmphasis(string text)
    {
        var withoutEmpty = EmptyBold().Replace(text, "");
        withoutEmpty = EmptyItalic().Replace(withoutEmpty, "");
        var escaped = withoutEmpty.Replace("\\", "\\\\").Replace("*", "\\*");
        return escaped
            .Replace(BoldOpen, "**").Replace(BoldClose, "**")
            .Replace(ItalicOpen, "*").Replace(ItalicClose, "*");
    }

    [GeneratedRegex(@"\\(.)|\*{1,2}", RegexOptions.Singleline)]
    private static partial Regex EmphasisMarker();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"<(?:strong|b)(?:\s[^>]*)?>", RegexOptions.IgnoreCase)]
    private static partial Regex BoldOpenTag();

    [GeneratedRegex(@"</(?:strong|b)\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex BoldCloseTag();

    [GeneratedRegex(@"<(?:em|i)(?:\s[^>]*)?>", RegexOptions.IgnoreCase)]
    private static partial Regex ItalicOpenTag();

    [GeneratedRegex(@"</(?:em|i)\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex ItalicCloseTag();

    [GeneratedRegex("\uE001[ \\t]*\uE003")]
    private static partial Regex EmptyBold();

    [GeneratedRegex("\uE002[ \\t]*\uE004")]
    private static partial Regex EmptyItalic();

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
