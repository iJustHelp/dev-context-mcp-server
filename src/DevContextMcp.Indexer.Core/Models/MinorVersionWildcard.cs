using System.Globalization;

namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// Parses a minor-version wildcard entry such as "2.4.*" used in package version selection.
/// The wildcard selects the highest stable patch of the given major and minor.
/// </summary>
public static class MinorVersionWildcard
{
    /// <summary>
    /// Attempts to parse a "MAJOR.MINOR.*" wildcard. Returns false for any other form,
    /// including full versions and ranges.
    /// </summary>
    public static bool TryParse(string value, out int major, out int minor)
    {
        major = 0;
        minor = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Trim().Split('.');
        if (parts.Length != 3 || parts[2] != "*")
        {
            return false;
        }

        return int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out major)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minor);
    }
}
