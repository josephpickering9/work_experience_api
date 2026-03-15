using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Work_Experience_Search.Utils;

public static class StringExtensions
{
    public static string ToSlug(this string phrase)
    {
        var output = phrase.ToLowerInvariant();
        output = RemoveDiacritics(output);
        output = Regex.Replace(output, @"\s", "-", RegexOptions.Compiled);
        output = Regex.Replace(output, @"[^a-z0-9\s-_]", "", RegexOptions.Compiled);
        output = output.Trim('-', '_');
        output = Regex.Replace(output, @"([-_]){2,}", "$1", RegexOptions.Compiled);
        return output;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark) stringBuilder.Append(c);
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
