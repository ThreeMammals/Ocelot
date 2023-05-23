using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.File;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Security.IPSecurity;

namespace Ocelot.UnitTests.Security
{
    public class IPSecurityPolicyTests
    {
        private readonly DownstreamRouteBuilder _downstreamRouteBuilder;
        private readonly IPSecurityPolicy _ipSecurityPolicy;
        private Response response;
        private readonly HttpContext _httpContext;

        public IPSecurityPolicyTests()
        {
            _httpContext = new DefaultHttpContext();
            _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com")));
            _httpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
            _downstreamRouteBuilder = new DownstreamRouteBuilder();
            _ipSecurityPolicy = new IPSecurityPolicy();
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

        /// <summary>
        /// NO
        /// </summary>
        private void GivenSetAllowedIP()
        {
            _downstreamRouteBuilder.WithSecurityOptions(new SecurityOptions(new List<string> { "192.168.1.1" }, new List<string>()));
        }

        /// <summary>
        /// NO
        /// </summary>
        private void GivenSetBlockedIP()
        {
            _downstreamRouteBuilder.WithSecurityOptions(new SecurityOptions(new List<string>(), new List<string> { "192.168.1.1" }));
        }

        private void GivenSetDownstreamRoute()
        {
            _httpContext.Items.UpsertDownstreamRoute(_downstreamRouteBuilder.Build());
        }

        private void GivenCidr24AllowedIP()
        {
            _downstreamRouteBuilder.WithSecurityOptionsCreator(new FileSecurityOptions { IPAllowedList = new List<string> { "192.168.1.0/24" }, IPBlockedList = new List<string>() });
        }

        private void GivenCidr29AllowedIP()
        {
            _downstreamRouteBuilder.WithSecurityOptionsCreator(new FileSecurityOptions { IPAllowedList = new List<string> { "192.168.1.0/29" }, IPBlockedList = new List<string>() });
        }

        private void GivenCidr24BlockedIP()
        {
            _downstreamRouteBuilder.WithSecurityOptionsCreator(new FileSecurityOptions { IPAllowedList = new List<string>(), IPBlockedList = new List<string> { "192.168.1.0/24" } });
        }

        private void GivenRangeAllowedIP()
        {
            _downstreamRouteBuilder.WithSecurityOptionsCreator(new FileSecurityOptions { IPAllowedList = new List<string> { "192.168.1.0-192.168.1.10" }, IPBlockedList = new List<string>() });
        }

        private void GivenRangeBlockedIP()
        {
            _downstreamRouteBuilder.WithSecurityOptionsCreator(new FileSecurityOptions { IPAllowedList = new List<string>(), IPBlockedList = new List<string> { "192.168.1.0-192.168.1.10" } });
        }

        private void GivenShortRangeAllowedIP()
        {
            _downstreamRouteBuilder.WithSecurityOptionsCreator(new FileSecurityOptions { IPAllowedList = new List<string> { "192.168.1.0-10" }, IPBlockedList = new List<string>() });
        }

        private void GivenShortRangeBlockedIP()
        {
            _downstreamRouteBuilder.WithSecurityOptionsCreator(new FileSecurityOptions { IPAllowedList = new List<string>(), IPBlockedList = new List<string> { "192.168.1.0-10" } });
        }

        private void GivenIpSubnetAllowedIP()
        {
            _downstreamRouteBuilder.WithSecurityOptionsCreator(new FileSecurityOptions { IPAllowedList = new List<string> { "192.168.1.0/255.255.255.0" }, IPBlockedList = new List<string>() });
        }

        private void GivenIpSubnetBlockedIP()
        {
            _downstreamRouteBuilder.WithSecurityOptionsCreator(new FileSecurityOptions { IPAllowedList = new List<string>(), IPBlockedList = new List<string> { "192.168.1.0/255.255.255.0" } });
        }

        private void GivenIpMoreAllowedThanBlocked(bool excludeAllowedInBlocked)
        {
            _downstreamRouteBuilder.WithSecurityOptionsCreator(new FileSecurityOptions { IPAllowedList = new List<string> { "192.168.0.0/255.255.0.0" }, IPBlockedList = new List<string> { "192.168.1.100-200" }, ExcludeAllowedFromBlocked = excludeAllowedInBlocked });
        }

        private void GivenIpMoreBlockedThanAllowed(bool excludeAllowedInBlocked)
        {
            _downstreamRouteBuilder.WithSecurityOptionsCreator(new FileSecurityOptions { IPAllowedList = new List<string> { "192.168.1.10-20" }, IPBlockedList = new List<string> { "192.168.1.0/23" }, ExcludeAllowedFromBlocked = excludeAllowedInBlocked });
        }

        private void WhenTheSecurityPolicy()
        {
            response = _ipSecurityPolicy.Security(_httpContext.Items.DownstreamRoute(), _httpContext).GetAwaiter().GetResult();
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
