using Microsoft.Extensions.Configuration;

namespace DevContextMcp.Server.Retrieval;

internal static class RecommendedVersionsConfigurationReader
{
    public static Dictionary<string, string> Read(IConfigurationSection section)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddValues(section, prefix: null, values);
        return values;
    }

    private static void AddValues(
        IConfigurationSection section,
        string? prefix,
        IDictionary<string, string> values)
    {
        foreach (var child in section.GetChildren())
        {
            var key = prefix is null ? child.Key : $"{prefix}:{child.Key}";
            if (child.Value is not null)
            {
                values[key] = child.Value;
            }

            AddValues(child, key, values);
        }
    }
}
