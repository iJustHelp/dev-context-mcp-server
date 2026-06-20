using System.Net;
using System.Security.Claims;
using DevContextMcp.Server.Analytics;
using DevContextMcp.Server.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace DevContextMcp.UnitTests.Analytics;

public sealed class AnalyticsUserResolverTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly DevContextMcpOptions _options = new();
    private readonly AnalyticsUserResolver _target;

    public AnalyticsUserResolverTests()
    {
        _target = new AnalyticsUserResolver(
            _httpContextAccessor.Object,
            Options.Create(_options));
    }

    // Purpose: returns the trimmed configured header when present
    [Fact]
    public void Resolve_HeaderPresent_ReturnsTrimmedHeader()
    {
        // arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Name"] = " alice ";
        context.User = Principal("identity-user");
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        _httpContextAccessor
            .Setup(accessor => accessor.HttpContext)
            .Returns(context);

        // act
        var actual = _target.Resolve();

        // assert
        Assert.Equal("alice", actual);
        VerifyHttpContextRead();
    }

    // Purpose: falls back to the authenticated identity when no header is present
    [Fact]
    public void Resolve_NoHeaderButIdentity_ReturnsIdentity()
    {
        // arrange
        var context = new DefaultHttpContext { User = Principal("identity-user") };
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        _httpContextAccessor
            .Setup(accessor => accessor.HttpContext)
            .Returns(context);

        // act
        var actual = _target.Resolve();

        // assert
        Assert.Equal("identity-user", actual);
        VerifyHttpContextRead();
    }

    // Purpose: falls back to the remote IP when no header or identity is present
    [Fact]
    public void Resolve_NoHeaderOrIdentity_ReturnsRemoteIp()
    {
        // arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.1.2.3");
        _httpContextAccessor
            .Setup(accessor => accessor.HttpContext)
            .Returns(context);

        // act
        var actual = _target.Resolve();

        // assert
        Assert.Equal("10.1.2.3", actual);
        VerifyHttpContextRead();
    }

    // Purpose: returns anonymous when the context carries no identifying values
    [Fact]
    public void Resolve_NoContextValues_ReturnsAnonymous()
    {
        // arrange
        _httpContextAccessor
            .Setup(accessor => accessor.HttpContext)
            .Returns(new DefaultHttpContext());

        // act
        var actual = _target.Resolve();

        // assert
        Assert.Equal("anonymous", actual);
        VerifyHttpContextRead();
    }

    // Purpose: returns anonymous when there is no HTTP context
    [Fact]
    public void Resolve_NoHttpContext_ReturnsAnonymous()
    {
        // arrange
        _httpContextAccessor
            .Setup(accessor => accessor.HttpContext)
            .Returns((HttpContext?)null);

        // act
        var actual = _target.Resolve();

        // assert
        Assert.Equal("anonymous", actual);
        VerifyHttpContextRead();
    }

    // Purpose: reads the header named in configuration
    [Fact]
    public void Resolve_ConfiguredHeaderName_ReadsThatHeader()
    {
        // arrange
        _options.Analytics.UserHeaderName = "X-Caller";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Caller"] = "carol";
        _httpContextAccessor
            .Setup(accessor => accessor.HttpContext)
            .Returns(context);

        // act
        var actual = _target.Resolve();

        // assert
        Assert.Equal("carol", actual);
        VerifyHttpContextRead();
    }

    private void VerifyHttpContextRead()
    {
        _httpContextAccessor.VerifyGet(accessor => accessor.HttpContext, Times.Once);
        _httpContextAccessor.VerifyNoOtherCalls();
    }

    private static ClaimsPrincipal Principal(string name) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, name)],
            authenticationType: "test"));
}
