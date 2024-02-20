using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Work_Experience_Search.Utils;

public static class DatabaseExtensions
{
    private static readonly IReadOnlyDictionary<char, string> PatternMapping = new Dictionary<char, string>
    {
        { '%', ".*" },
        { '_', "?*" },
        { '\\', "" }
    };

    public static bool ILike(string input, string pattern)
    {
        return UnitTestExtensions.IsInUnitTest() ? InMemoryILike(input, pattern) : EF.Functions.ILike(input, pattern);
    }

    private static bool InMemoryILike(string input, string sqlPattern)
    {
        var capacity = sqlPattern.Length + sqlPattern.Length / 2;
        var stringBuilder = new StringBuilder(capacity);

        foreach (var character in sqlPattern)
            if (PatternMapping.TryGetValue(character, out var newCharacters))
                stringBuilder.Append(newCharacters);
            else
                stringBuilder.Append(character);

        var regexPattern = stringBuilder.ToString();
        var regex = new Regex(
            regexPattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(50)
        );

        return regex.IsMatch(input);
    }
}
