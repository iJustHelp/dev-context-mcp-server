namespace DevContextMcp.Server.Configuration;

internal static class ConfigurationValidation
{
    public static void ValidatePath(
        string value,
        string fieldName,
        List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{fieldName} must not be empty.");
            return;
        }

        try
        {
            _ = Path.GetFullPath(value);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            failures.Add($"{fieldName} is not a valid path: {exception.Message}");
        }
    }
}
