using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Security;
using System.Security.Cryptography.X509Certificates;

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
    public void Should_return_OkResponse_when_options_null()
    {
        // Arrange
        _downstreamRouteBuilder.WithSecurityOptions(null);
        _context.Items.UpsertDownstreamRoute(_downstreamRouteBuilder.Build());

        var actual = _policy.Security(_context.Items.DownstreamRoute(), _context);

        // Assert
        Assert.False(actual.IsError);
        Assert.IsType<OkResponse>(actual);
    }

    [Fact]
    public void Should_return_OkResponse_when_RemoteIpAddress_null()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = null;
        var options = new FileSecurityOptions();

        // Act
        var actual = WhenTheSecurityPolicy(options);

        // Assert
        Assert.False(actual.IsError);
        Assert.IsType<OkResponse>(actual);
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

    private void SetupContext(FileSecurityOptions options, FileGlobalConfiguration global = null)
    {
        var securityOptions = _securityOptionsCreator.Create(options, global ?? Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        _context.Items.UpsertDownstreamRoute(_downstreamRouteBuilder.Build());
    }

    private Response WhenTheSecurityPolicy(FileSecurityOptions options, FileGlobalConfiguration global = null)
    {
        SetupContext(options, global);
        return _policy.Security(_context.Items.DownstreamRoute(), _context);
    }
    private Task<Response> WhenTheSecurityPolicyAsync(FileSecurityOptions options, FileGlobalConfiguration global = null)
    {
        SetupContext(options, global);
        return _policy.SecurityAsync(_context.Items.DownstreamRoute(), _context);
    }

    #region PR 2392
    [Fact]
    public async Task Should_SecurityAsync_No_blocked_Ip_and_allowed_Ip()
    {
        // Arrange
        var options = new FileSecurityOptions();

        // Act
        var actual = await WhenTheSecurityPolicyAsync(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public async Task Should_SecurityAsync_blockedIp_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.1");

        // Act
        var actual = await WhenTheSecurityPolicyAsync(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public async Task Should_SecurityAsync_allowedIp_clientIp_allow()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
        var options = new FileSecurityOptions("192.168.1.1");

        // Act
        var actual = await WhenTheSecurityPolicyAsync(options);

        // Assert
        Assert.False(actual.IsError);
    }

    [Fact]
    public async Task Should_SecurityAsync_allowedIp_clientIp_block()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.2")[0];
        var options = new FileSecurityOptions("192.168.1.1");

        // Act
        var actual = await WhenTheSecurityPolicyAsync(options);

        // Assert
        Assert.True(actual.IsError);
    }

    [Fact]
    public async Task Should_SecurityAsync_respect_request_cancellation_token()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _context.RequestAborted = cts.Token;
        var options = new FileSecurityOptions();
        
        // Create a mock context that introduces delay when accessing RemoteIpAddress
        // This ensures the Task.Run has time to be cancelled before the sync method completes
        const int RemoteIpAddressTimeoutMilliseconds = 100; // 100ms delay
        var mockConnection = new MockConnectionInfo(_context.Connection.RemoteIpAddress, RemoteIpAddressTimeoutMilliseconds);
        var contextWithDelay = new DefaultHttpContext()
        {
            RequestAborted = cts.Token
        };
        contextWithDelay.Features.Set<ConnectionInfo>(mockConnection);
        contextWithDelay.Items.UpsertDownstreamRequest(_context.Items.DownstreamRequest());
        
        var securityOptions = _securityOptionsCreator.Create(options, Empty);
        _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        contextWithDelay.Items.UpsertDownstreamRoute(_downstreamRouteBuilder.Build());

        // Act
        var securityAsync = _policy.SecurityAsync(contextWithDelay.Items.DownstreamRoute(), contextWithDelay);
        cts.Cancel();

        // Assert - Should handle cancellation gracefully and throw TaskCanceledException
        var ex = await Record.ExceptionAsync(() => securityAsync);
        Assert.NotNull(ex);
        Assert.IsType<TaskCanceledException>(ex);
    }

    [Fact]
    public async Task Should_SecurityAsync_returns_ok_response_when_security_passes()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
        var options = new FileSecurityOptions();

        // Act
        var actual = await WhenTheSecurityPolicyAsync(options);

        // Assert
        Assert.IsType<OkResponse>(actual);
        Assert.False(actual.IsError);
    }

    [Fact]
    public async Task Should_SecurityAsync_returns_error_response_when_blocked()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
        var options = new FileSecurityOptions(blockedIPs: "192.168.1.1");

        // Act
        var actual = await WhenTheSecurityPolicyAsync(options);

        // Assert
        Assert.IsType<ErrorResponse>(actual);
        Assert.True(actual.IsError);
    }
    #endregion

    /// <summary>
    /// Mock connection that introduces a 100ms delay when accessing RemoteIpAddress.
    /// This gives the cancellation token time to trigger before the synchronous Security() method completes.
    /// </summary>
    private class MockConnectionInfo(IPAddress ipAddress, int remoteIpAddressTimeout) : ConnectionInfo
    {
        private readonly IPAddress _ipAddress = ipAddress;
        private readonly int _remoteIpAddressTimeout = remoteIpAddressTimeout;

        public override IPAddress RemoteIpAddress
        {
            get
            {
                Thread.Sleep(_remoteIpAddressTimeout);
                return _ipAddress;
            }
            set { }
        }

        public override string Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override int RemotePort { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IPAddress LocalIpAddress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override int LocalPort { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override X509Certificate2 ClientCertificate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
