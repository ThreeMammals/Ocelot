using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Security.IPSecurity;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Security
{
    public class IPSecurityPolicyTests
    {
        private readonly DownstreamContext _downstreamContext;
        private readonly DownstreamReRouteBuilder _downstreamReRouteBuilder;
        private readonly IPSecurityPolicy _ipSecurityPolicy;
        private Response response;

        public IPSecurityPolicyTests()
        {
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _downstreamContext.DownstreamRequest = new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com"));
            _downstreamContext.HttpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
            _downstreamReRouteBuilder = new DownstreamReRouteBuilder();
            _ipSecurityPolicy = new IPSecurityPolicy();
        }

        [Fact]
        private void should_No_blocked_Ip_and_allowed_Ip()
        {
            this.Given(x => x.GivenSetDownstreamReRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        private void should_blockedIp_clientIp_block()
        {
            _downstreamContext.HttpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
            this.Given(x => x.GivenSetBlockedIP())
                .Given(x => x.GivenSetDownstreamReRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        [Fact]
        private void should_blockedIp_clientIp_Not_block()
        {
            _downstreamContext.HttpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.2")[0];
            this.Given(x => x.GivenSetBlockedIP())
                .Given(x => x.GivenSetDownstreamReRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        private void should_allowedIp_clientIp_block()
        {
            _downstreamContext.HttpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.1")[0];
            this.Given(x => x.GivenSetAllowedIP())
                .Given(x => x.GivenSetDownstreamReRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenSecurityPassing())
                .BDDfy();
        }

        [Fact]
        private void should_allowedIp_clientIp_Not_block()
        {
            _downstreamContext.HttpContext.Connection.RemoteIpAddress = Dns.GetHostAddresses("192.168.1.2")[0];
            this.Given(x => x.GivenSetAllowedIP())
                .Given(x => x.GivenSetDownstreamReRoute())
                .When(x => x.WhenTheSecurityPolicy())
                .Then(x => x.ThenNotSecurityPassing())
                .BDDfy();
        }

        private void GivenSetAllowedIP()
        {
            _downstreamReRouteBuilder.WithSecurityOptions(new SecurityOptions(new List<string> { "192.168.1.1" }, new List<string>()));
        }

        private void GivenSetBlockedIP()
        {
            _downstreamReRouteBuilder.WithSecurityOptions(new SecurityOptions(new List<string>(), new List<string> { "192.168.1.1" }));
        }

        private void GivenSetDownstreamReRoute()
        {
            _downstreamContext.DownstreamReRoute = _downstreamReRouteBuilder.Build();
        }

        private void WhenTheSecurityPolicy()
        {
            response = this._ipSecurityPolicy.Security(_downstreamContext).GetAwaiter().GetResult();
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
