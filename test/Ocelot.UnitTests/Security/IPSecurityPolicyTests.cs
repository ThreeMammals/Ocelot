using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Security.IPSecurity;

namespace Ocelot.UnitTests.Security
{
    public class IPSecurityPolicyTests : UnitTest
    {
        private readonly DownstreamRouteBuilder _downstreamRouteBuilder;
        private readonly IPSecurityPolicy _ipSecurityPolicy;
        private Response response;
        private readonly HttpContext _httpContext;
        private readonly SecurityOptionsCreator _securityOptionsCreator;
        private readonly FileGlobalConfiguration _emptyFileGlobalConfiguration;

        public IPSecurityPolicyTests()
        {
            _httpContext = new DefaultHttpContext();
            _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com")));
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
            _downstreamRouteBuilder = new DownstreamRouteBuilder();
            _ipSecurityPolicy = new IPSecurityPolicy();
            _securityOptionsCreator = new SecurityOptionsCreator();
            _emptyFileGlobalConfiguration = new FileGlobalConfiguration();
        }

        [Fact]
        public void should_No_blocked_Ip_and_allowed_Ip()
        {
            this.Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_blockedIp_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
            this.Given(x => x.GivenSetBlockedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_blockedIp_clientIp_Not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.2")[0];
            this.Given(x => x.GivenSetBlockedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_allowedIp_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
            this.Given(x => x.GivenSetAllowedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_allowedIp_clientIp_Not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.2")[0];
            this.Given(x => x.GivenSetAllowedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_cidrNotation_allowed24_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.5")[0];
            this.Given(x => x.GivenCidr24AllowedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_cidrNotation_allowed24_clientIp_not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.5")[0];
            this.Given(x => x.GivenCidr24AllowedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_cidrNotation_allowed29_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
            this.Given(x => x.GivenCidr29AllowedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_cidrNotation_blocked24_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
            this.Given(x => x.GivenCidr24BlockedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_cidrNotation_blocked24_clientIp_not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.1")[0];
            this.Given(x => x.GivenCidr24BlockedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_range_allowed_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
            this.Given(x => x.GivenRangeAllowedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_range_allowed_clientIp_not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.8")[0];
            this.Given(x => x.GivenRangeAllowedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_range_blocked_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.5")[0];
            this.Given(x => x.GivenRangeBlockedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_range_blocked_clientIp_not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
            this.Given(x => x.GivenRangeBlockedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_shortRange_allowed_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
            this.Given(x => x.GivenShortRangeAllowedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_shortRange_allowed_clientIp_not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.8")[0];
            this.Given(x => x.GivenShortRangeAllowedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_shortRange_blocked_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.5")[0];
            this.Given(x => x.GivenShortRangeBlockedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_shortRange_blocked_clientIp_not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
            this.Given(x => x.GivenShortRangeBlockedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_ipSubnet_allowed_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.15")[0];
            this.Given(x => x.GivenIpSubnetAllowedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_ipSubnet_allowed_clientIp_not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
            this.Given(x => x.GivenIpSubnetAllowedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_ipSubnet_blocked_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.15")[0];
            this.Given(x => x.GivenIpSubnetBlockedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_ipSubnet_blocked_clientIp_not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.10.1")[0];
            this.Given(x => x.GivenIpSubnetBlockedIP())
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_exludeAllowedFromBlocked_moreAllowed_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.150")[0];
            this.Given(x => x.GivenIpMoreAllowedThanBlocked(false))
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_exludeAllowedFromBlocked_moreAllowed_clientIp_not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.150")[0];
            this.Given(x => x.GivenIpMoreAllowedThanBlocked(true))
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_exludeAllowedFromBlocked_moreBlocked_clientIp_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
            this.Given(x => x.GivenIpMoreBlockedThanAllowed(false))
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_exludeAllowedFromBlocked_moreBlocked_clientIp_not_block()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
            this.Given(x => x.GivenIpMoreBlockedThanAllowed(true))
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        public void should_route_config_overrides_global_config()
        {
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.10")[0];
            this.Given(x => x.GivenRouteConfigAndGlobalConfig(false))
                .Given(x => x.GivenSetDownstreamRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
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
            _httpContext.Items.UpsertDownstreamRoute(_downstreamRouteBuilder.Build());
        }

        private void GivenCidr24AllowedIP()
        {
            var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.0/24"), _emptyFileGlobalConfiguration);
            _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        }

        private void GivenCidr29AllowedIP()
        {
            var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.0/29"), _emptyFileGlobalConfiguration);
            _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        }

        private void GivenCidr24BlockedIP()
        {
            var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions(blockedIPs: "192.168.1.0/24"), _emptyFileGlobalConfiguration);
            _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        }

        private void GivenRangeAllowedIP()
        {
            var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.0-192.168.1.10"), _emptyFileGlobalConfiguration);
            _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        }

        private void GivenRangeBlockedIP()
        {
            var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions(blockedIPs: "192.168.1.0-192.168.1.10"), _emptyFileGlobalConfiguration);
            _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        }

        private void GivenShortRangeAllowedIP()
        {
            var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.0-10"), _emptyFileGlobalConfiguration);
            _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        }

        private void GivenShortRangeBlockedIP()
        {
            var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions(blockedIPs: "192.168.1.0-10"), _emptyFileGlobalConfiguration);
            _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        }

        private void GivenIpSubnetAllowedIP()
        {
            var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.0/255.255.255.0"), _emptyFileGlobalConfiguration);
            _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        }

        private void GivenIpSubnetBlockedIP()
        {
            var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions(blockedIPs: "192.168.1.0/255.255.255.0"), _emptyFileGlobalConfiguration);
            _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        }

        private void GivenIpMoreAllowedThanBlocked(bool excludeAllowedInBlocked)
        {
            var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.0.0/255.255.0.0", "192.168.1.100-200", excludeAllowedInBlocked), _emptyFileGlobalConfiguration);
            _downstreamRouteBuilder.WithSecurityOptions(securityOptions);
        }

        private void GivenIpMoreBlockedThanAllowed(bool excludeAllowedInBlocked)
        {
            var securityOptions = _securityOptionsCreator.Create(new FileSecurityOptions("192.168.1.10-20", "192.168.1.0/23", excludeAllowedInBlocked), _emptyFileGlobalConfiguration);
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

        private async Task WhenTheSecurityPolicy()
        {
            response = await _ipSecurityPolicy.Security(_httpContext.Items.DownstreamRoute(), _httpContext);
        }

        private void ThenSecurityPassing()
        {
            Assert.False(response.IsError);
        }

        private void ThenNotSecurityPassing()
        {
            Assert.True(response.IsError);
        }
    }
}
