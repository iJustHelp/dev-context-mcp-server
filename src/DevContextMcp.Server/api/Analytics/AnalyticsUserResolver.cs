using DevContextMcp.Server.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Server.api.Analytics;

/// <summary>
/// Resolves the audited caller identity for an invocation. This is a trust-the-header
/// model: the configured header, then the authenticated identity, then the remote IP,
/// then <c>anonymous</c>. The value is never used for authorization.
/// </summary>
internal sealed class AnalyticsUserResolver(
    IHttpContextAccessor httpContextAccessor,
    IOptions<DevContextMcpOptions> options)
{
    private const string Anonymous = "anonymous";

    public string Resolve()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return Anonymous;
        }

        var header = context.Request.Headers[options.Value.Analytics.UserHeaderName].ToString();
        if (!string.IsNullOrWhiteSpace(header))
        {
            return header.Trim();
        }

        var identity = context.User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(identity))
        {
            return identity.Trim();
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(remoteIp) ? Anonymous : remoteIp;
    }
}
