namespace DevContextMcp.Server.Configuration;

/// <summary>
/// Configuration for MCP tool-call analytics capture and the analytics API.
/// </summary>
public sealed class AnalyticsOptions
{
    /// <summary>
    /// When false, no capture, background writer, or analytics endpoints are registered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Path to the self-creating analytics database, resolved relative to the host
    /// base directory. Kept separate from the read-only documentation index.
    /// </summary>
    public string DatabasePath { get; set; } = "data/analytics.db";

    /// <summary>
    /// Request header carrying the audited caller identity.
    /// </summary>
    public string UserHeaderName { get; set; } = "X-User-Name";
}
