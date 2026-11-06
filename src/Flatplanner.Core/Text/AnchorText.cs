using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Flatplanner.Core.Text;

public static partial class AnchorText
{
    public static string Normalize(string value)
    {
        var text = WebUtility.HtmlDecode(value).Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            builder.Append(c switch
            {
                ' ' or ' ' or ' ' or ' ' or '\t' or '\r' or '\n' => " ",
                '‘' or '’' or '‚' or '‛' => "'",
                '“' or '”' or '„' or '‟' or '«' or '»' => "\"",
                '–' or '—' or '‒' or '−' => "-",
                '…' => "...",
                _ => c.ToString(),
            });
        }

        var folded = TextFolding.RemoveDiacritics(builder.ToString()).ToLowerInvariant();
        var collapsed = Whitespace().Replace(folded, " ").Trim();
        return SpaceBetweenDigits().Replace(collapsed, "");
    }

    public static string NormalizeQuote(string quote) => Normalize(quote).TrimEnd('.', ',', ';', ':', '!');

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"(?<=\d) (?=\d)")]
    private static partial Regex SpaceBetweenDigits();
}
