using System.Security.Claims;
using DevContextMcp.Server.Analytics;
using DevContextMcp.Server.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DevContextMcp.UnitTests.Analytics;

public sealed class AnalyticsUserResolverTests
{
    [Fact]
    public void HeaderTakesPrecedence()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Name"] = " alice ";
        context.User = Principal("identity-user");
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;

        Assert.Equal("alice", Resolve(context));
    }

    [Fact]
    public void FallsBackToAuthenticatedIdentity()
    {
        var context = new DefaultHttpContext { User = Principal("identity-user") };
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;

        Assert.Equal("identity-user", Resolve(context));
    }

    [Fact]
    public void FallsBackToRemoteIp()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.1.2.3");

        Assert.Equal("10.1.2.3", Resolve(context));
    }

    [Fact]
    public void FallsBackToAnonymous()
    {
        Assert.Equal("anonymous", Resolve(new DefaultHttpContext()));
    }

    [Fact]
    public void AnonymousWhenNoHttpContext()
    {
        Assert.Equal("anonymous", Resolve(httpContext: null));
    }

    [Fact]
    public void UsesConfiguredHeaderName()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Caller"] = "carol";

        var resolver = new AnalyticsUserResolver(
            new HttpContextAccessor { HttpContext = context },
            Options.Create(new DevContextMcpOptions
            {
                Analytics = new AnalyticsOptions { UserHeaderName = "X-Caller" }
            }));

        Assert.Equal("carol", resolver.Resolve());
    }

    private static string Resolve(HttpContext? httpContext) =>
        new AnalyticsUserResolver(
            new HttpContextAccessor { HttpContext = httpContext },
            Options.Create(new DevContextMcpOptions())).Resolve();

    private static ClaimsPrincipal Principal(string name) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, name)],
            authenticationType: "test"));
}
