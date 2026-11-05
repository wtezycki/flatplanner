using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Flatplanner.Core.Text;

public static partial class TextFolding
{
    public static string Fold(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(c switch { 'ł' => 'l', 'Ł' => 'L', _ => c });
        }

        return NonAlphanumeric().Replace(builder.ToString().ToLowerInvariant(), " ").Trim();
    }

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NonAlphanumeric();
}
