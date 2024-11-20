using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
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
    private readonly HttpContext _context;
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
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_blockedIp_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
        GivenSetBlockedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_blockedIp_clientIp_Not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.2")[0];
        GivenSetBlockedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_allowedIp_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
        GivenSetAllowedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_allowedIp_clientIp_Not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.2")[0];
        GivenSetAllowedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_cidrNotation_allowed24_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.5")[0];
        GivenCidr24AllowedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_cidrNotation_allowed24_clientIp_not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.5")[0];
        GivenCidr24AllowedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_cidrNotation_allowed29_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
        GivenCidr29AllowedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_cidrNotation_blocked24_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
        GivenCidr24BlockedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_cidrNotation_blocked24_clientIp_not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.1")[0];
        GivenCidr24BlockedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_range_allowed_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        GivenRangeAllowedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_range_allowed_clientIp_not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.8")[0];
        GivenRangeAllowedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_range_blocked_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.5")[0];
        GivenRangeBlockedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_range_blocked_clientIp_not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        GivenRangeBlockedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_shortRange_allowed_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        GivenShortRangeAllowedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_shortRange_allowed_clientIp_not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.8")[0];
        GivenShortRangeAllowedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_shortRange_blocked_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.5")[0];
        GivenShortRangeBlockedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_shortRange_blocked_clientIp_not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        GivenShortRangeBlockedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_ipSubnet_allowed_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.15")[0];
        GivenIpSubnetAllowedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_ipSubnet_allowed_clientIp_not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        GivenIpSubnetAllowedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_ipSubnet_blocked_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
        GivenIpSubnetBlockedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_ipSubnet_blocked_clientIp_not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.1")[0];
        GivenIpSubnetBlockedIP();
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_exludeAllowedFromBlocked_moreAllowed_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.150")[0];
        GivenIpMoreAllowedThanBlocked(false);
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_exludeAllowedFromBlocked_moreAllowed_clientIp_not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.150")[0];
        GivenIpMoreAllowedThanBlocked(true);
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    public void Should_exludeAllowedFromBlocked_moreBlocked_clientIp_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
        GivenIpMoreBlockedThanAllowed(false);
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.True(actual.IsError);
    }

    [Fact]
    public void Should_exludeAllowedFromBlocked_moreBlocked_clientIp_not_block()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
        GivenIpMoreBlockedThanAllowed(true);
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    [Fact]
    [Trait("Feat", "2170")]
    public void Should_route_config_overrides_global_config()
    {
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
        GivenRouteConfigAndGlobalConfig(false);
        GivenSetDownstreamRoute();
        var actual = WhenTheSecurityPolicy();
        Assert.False(actual.IsError);
    }

    private void GivenSetAllowedIP()
    {
        _downstreamRouteBuilder.WithSecurityOptions(new SecurityOptions("192.168.1.1"));
    }

    private void GivenSetBlockedIP()
    {
        _downstreamRouteBuilder.WithSecurityOptions(new SecurityOptions(blocked: "192.168.1.1"));
    }

    private void GivenSetDownstreamRoute()
    {
        _context.Items.UpsertDownstreamRoute(_downstreamRouteBuilder.Build());
    }

    private void GivenCidr24AllowedIP()
    {
        var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.0/24"), Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private void GivenCidr29AllowedIP()
    {
        var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.0/29"), Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private void GivenCidr24BlockedIP()
    {
        var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions(blockedIPs: "192.168.1.0/24"), Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private void GivenRangeAllowedIP()
    {
        var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.0-192.168.1.10"), Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private void GivenRangeBlockedIP()
    {
        var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions(blockedIPs: "192.168.1.0-192.168.1.10"), Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private void GivenShortRangeAllowedIP()
    {
        var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.0-10"), Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private void GivenShortRangeBlockedIP()
    {
        var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions(blockedIPs: "192.168.1.0-10"), Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private void GivenIpSubnetAllowedIP()
    {
        var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.0/255.255.255.0"), Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private void GivenIpSubnetBlockedIP()
    {
        var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions(blockedIPs: "192.168.1.0/255.255.255.0"), Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private void GivenIpMoreAllowedThanBlocked(bool excludeAllowedInBlocked)
    {
        var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.0.0/255.255.0.0", "192.168.1.100-200", excludeAllowedInBlocked), Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private void GivenIpMoreBlockedThanAllowed(bool excludeAllowedInBlocked)
    {
        var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.10-20", "192.168.1.0/23", excludeAllowedInBlocked), Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private void GivenRouteConfigAndGlobalConfig(bool excludeAllowedInBlocked)
    {
        var globalConfig = new FileGlobalConfiguration
        {
            SecurityOptions = new FileSecurityOptions("192.168.1.30-50", "192.168.1.1-100", true),
        };
        
        var localConfig = new FileSecurityOptions("192.168.1.10", "", excludeAllowedInBlocked);

        var securityOptions = _securityOptionsCreator.Create(localConfig, globalConfig);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
    }

    private Response WhenTheSecurityPolicy()
    {
        return _policy.Security(_context.Items.DownstreamRoute(), _context);
    }
}
