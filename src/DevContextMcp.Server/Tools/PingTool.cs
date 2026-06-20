using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace DevContextMcp.Server.Tools;

/// <summary>
/// Response returned by the <c>ping</c> connectivity check.
/// </summary>
/// <param name="Message">Human-readable greeting echoed back to the caller.</param>
/// <param name="User">The user the server greeted, taken from the request header.</param>
public sealed record PingResponse(string Message, string User);

/// <summary>
/// Minimal diagnostic MCP tool used to verify connectivity and configuration.
/// The client sends a greeting and the server replies with the user name
/// supplied in the <c>X-User-Name</c> request header, so it works regardless
/// of which machine the server runs on.
/// </summary>
[McpServerToolType]
internal sealed class PingTool(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>
    /// Request header the client uses to identify the calling user.
    /// </summary>
    public const string UserNameHeader = "X-User-Name";

    [McpServerTool(
        Name = "ping",
        UseStructuredContent = true,
        OutputSchemaType = typeof(PingResponse))]
    [Description("Connectivity check: replies with a greeting using the user name from the X-User-Name header.")]
    public PingResponse Ping()
    {
        var headerValue = httpContextAccessor.HttpContext?.Request
            .Headers[UserNameHeader]
            .ToString();

        var user = string.IsNullOrWhiteSpace(headerValue)
            ? "unknown user"
            : headerValue.Trim();

        return new PingResponse($"Hi, {user}!", user);
    }
}
