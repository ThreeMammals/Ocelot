using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Security.IPSecurity;

namespace Ocelot.UnitTests.Security;

public sealed class IPSecurityPolicyTests : UnitTest
{
    private readonly DownstreamRouteBuilder _downstreamRouteBuilder;
    private readonly IPSecurityPolicy _policy;
    private readonly DefaultHttpContext _context;
    private readonly SecurityOptionsCreator _securityOptionsCreator;
    private static readonly FileGlobalConfiguration Empty = new();

    public IPSecurityPolicyTests()
    {
        _context = new DefaultHttpContext();
        _context.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com")));
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
        _downstreamRouteBuilder = new DownstreamRouteBuilder();
        _policy = new IPSecurityPolicy();
        _securityOptionsCreator = new SecurityOptionsCreator();
    }

    [Fact]
    public void Should_No_blocked_Ip_and_allowed_Ip()
    {
        // Arrange, Act
        var actual = WhenTheSecurityPolicy(new());

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_blockedIp_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.1");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_blockedIp_clientIp_Not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.2")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.1");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_allowedIp_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
        var options = new FileSecurityOptions("192.168.1.1");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_allowedIp_clientIp_Not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.2")[0];
        var options = new FileSecurityOptions("192.168.1.1");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_cidrNotation_allowed24_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.5")[0];
        var options = new FileSecurityOptions("192.168.1.0/24");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_cidrNotation_allowed24_clientIp_not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.5")[0];
        var options = new FileSecurityOptions("192.168.1.0/24");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_cidrNotation_allowed29_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
        var options = new FileSecurityOptions("192.168.1.0/29");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_cidrNotation_blocked24_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.0/24");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_cidrNotation_blocked24_clientIp_not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.1")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.0/24");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_range_allowed_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        var options = new FileSecurityOptions("192.168.1.0-192.168.1.10");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_range_allowed_clientIp_not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.8")[0];
        var options = new FileSecurityOptions("192.168.1.0-192.168.1.10");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_range_blocked_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.5")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.0-192.168.1.10");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_range_blocked_clientIp_not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.0-192.168.1.10");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_shortRange_allowed_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        var options = new FileSecurityOptions("192.168.1.0-10");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_shortRange_allowed_clientIp_not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.8")[0];
        var options = new FileSecurityOptions("192.168.1.0-10");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_shortRange_blocked_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.5")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.0-10");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_shortRange_blocked_clientIp_not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.0-10");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_ipSubnet_allowed_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.15")[0];
        var options = new FileSecurityOptions("192.168.1.0/255.255.255.0");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_ipSubnet_allowed_clientIp_not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        var options = new FileSecurityOptions("192.168.1.0/255.255.255.0");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_ipSubnet_blocked_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.0/255.255.255.0");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_ipSubnet_blocked_clientIp_not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.1")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.0/255.255.255.0");

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_exludeAllowedFromBlocked_moreAllowed_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.150")[0];
        var options = new FileSecurityOptions("192.168.0.0/255.255.0.0", "192.168.1.100-200", false);

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_exludeAllowedFromBlocked_moreAllowed_clientIp_not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.150")[0];
        var options = new FileSecurityOptions("192.168.0.0/255.255.0.0", "192.168.1.100-200", true);

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_exludeAllowedFromBlocked_moreBlocked_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
        var options = new FileSecurityOptions("192.168.1.10-20", "192.168.1.0/23", false);

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_exludeAllowedFromBlocked_moreBlocked_clientIp_not_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
        var options = new FileSecurityOptions("192.168.1.10-20", "192.168.1.0/23", true);

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    [Trait("Feat", "2170")]
    public void Should_route_config_overrides_global_config()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
        var globalConfig = new FileGlobalConfiguration
        {
            SecurityOptions = new FileSecurityOptions("192.168.1.30-50", "192.168.1.1-100", true),
        };
        var localConfig = new FileSecurityOptions("192.168.1.10", "", false);

        // Act
        var actual = WhenTheSecurityPolicy(localConfig, globalConfig);

        // Assert
        Assert.False(actual.IsError);
    }

    private Response WhenTheSecurityPolicy(FileSecurityOptions options, FileGlobalConfiguration global = null)
    {
        // Arrange
        var securityOptions = _securityOptionsCreator.Create(options, global ?? Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        _context.Items.UpsertDownstreamRoute(_downstreamRouteBuilder.Build());

        // Act
        return _policy.Security(_context.Items.DownstreamRoute(), _context);
    }
}
