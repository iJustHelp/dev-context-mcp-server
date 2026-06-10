namespace DevContextMcp.Server.Configuration;

/// <summary>
/// Local Streamable HTTP host configuration.
/// </summary>
public sealed class HttpHostOptions
{
    public string Url { get; set; } = "http://127.0.0.1:5034";

    public string Path { get; set; } = "/mcp";
}
