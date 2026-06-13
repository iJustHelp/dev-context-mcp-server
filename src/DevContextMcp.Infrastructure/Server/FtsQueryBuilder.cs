using System.Text.RegularExpressions;

namespace DevContextMcp.Infrastructure.Server;

/// <summary>
/// Converts untrusted search text into a bounded SQLite FTS5 prefix query.
/// </summary>
internal static partial class FtsQueryBuilder
{
    public static string Build(string value)
    {
        // Extract only word-like terms so FTS operators and punctuation from
        // user input cannot change the structure of the generated query.
        var tokens = TokenPattern()
            .Matches(value)
            .Select(match => match.Value)
            .Where(token => token.Length > 0)
            // Bound query complexity and the amount of work requested from FTS.
            .Take(16)
            .Select(token =>
            {
                var escaped = token.Replace("\"", "\"\"", StringComparison.Ordinal);
                // Prefix matching improves recall for normal words. Single
                // characters remain exact to avoid extremely broad matches.
                return token.Length >= 2 ? $"\"{escaped}\"*" : $"\"{escaped}\"";
            })
            .ToArray();

        return string.Join(" AND ", tokens);
    }

    // Unicode letters, numbers, and underscores are the only accepted terms.
    [GeneratedRegex(@"[\p{L}\p{N}_]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
