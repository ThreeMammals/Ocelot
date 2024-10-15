using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests.Security
{
    public sealed class SecurityOptionsTests: Steps
    {
        private readonly ServiceHandler _serviceHandler;

        public SecurityOptionsTests()
        {
            _serviceHandler = new ServiceHandler();
        }

        public override void Dispose()
        {
            _serviceHandler.Dispose();
            base.Dispose();
        }

        [Fact]
        public void should_call_with_allowed_ip_in_global_config()
        {
            var port = PortFinder.GetRandomPort();
            var ip = Dns.GetHostAddresses("192.168.1.35")[0];
            var route = GivenRoute(port, "/myPath", "/worldPath");
            var configuration = GivenConfigurationWithSecurityOptions(route);

            this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), ip))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning())
                .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK));
        }

        [Fact]
        public void should_block_call_with_blocked_ip_in_global_config()
        {
            var port = PortFinder.GetRandomPort();
            var ip = Dns.GetHostAddresses("192.168.1.55")[0];
            var route = GivenRoute(port, "/myPath", "/worldPath");
            var configuration = GivenConfigurationWithSecurityOptions(route);

            this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), ip))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning())
                .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized));
        }

        [Fact]
        public void should_call_with_allowed_ip_in_route_config()
        {
            var port = PortFinder.GetRandomPort();
            var ip = Dns.GetHostAddresses("192.168.1.1")[0];
            var securityConfig = new FileSecurityOptions
            {
                IPAllowedList = new List<string> { "192.168.1.1" },
            };

            var route = GivenRoute(port, "/myPath", "/worldPath", securityConfig);
            var configuration = GivenConfiguration(route);

            this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), ip))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning())
                .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK));
        }

        [Fact]
        public void should_block_call_with_blocked_ip_in_route_config()
        {
            var port = PortFinder.GetRandomPort();
            var ip = Dns.GetHostAddresses("192.168.1.1")[0];
            var securityConfig = new FileSecurityOptions
            {
                IPBlockedList = new List<string> { "192.168.1.1" },
            };

            var route = GivenRoute(port, "/myPath", "/worldPath", securityConfig);
            var configuration = GivenConfiguration(route);

            this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), ip))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning())
                .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized));
        }

        [Fact]
        public void should_call_with_allowed_ip_in_route_config_and_blocked_ip_in_global_config()
        {
            var port = PortFinder.GetRandomPort();
            var ip = Dns.GetHostAddresses("192.168.1.55")[0];
            var securityConfig = new FileSecurityOptions
            {
                IPAllowedList = new List<string> { "192.168.1.55" },
            };

            var route = GivenRoute(port, "/myPath", "/worldPath", securityConfig);
            var configuration = GivenConfigurationWithSecurityOptions(route);

            this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), ip))
               .And(x => GivenThereIsAConfiguration(configuration))
               .And(x => GivenOcelotIsRunning())
               .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
               .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK));
        }

        [Fact]
        public void should_block_call_with_blocked_ip_in_route_config_and_allowed_ip_in_global_config()
        {
            var port = PortFinder.GetRandomPort();
            var ip = Dns.GetHostAddresses("192.168.1.35")[0];
            var securityConfig = new FileSecurityOptions
            {
                IPBlockedList = new List<string> { "192.168.1.35" },
            };

            var route = GivenRoute(port, "/myPath", "/worldPath", securityConfig);
            var configuration = GivenConfigurationWithSecurityOptions(route);

            this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), ip))
               .And(x => GivenThereIsAConfiguration(configuration))
               .And(x => GivenOcelotIsRunning())
               .When(x => WhenIGetUrlOnTheApiGateway("/worldPath"))
               .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized));
        }

        private void GivenThereIsAServiceRunningOn(string url, IPAddress ipAddess)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
            {
                context.Connection.RemoteIpAddress = ipAddess;
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync("a valida response body");
            });
        }

        private static FileConfiguration GivenConfigurationWithSecurityOptions(params FileRoute[] routes)
        {
            var config = GivenConfiguration(routes);
            config.GlobalConfiguration.SecurityOptions = new FileSecurityOptions
            {
                IPAllowedList = new List<string> { "192.168.1.30-50" },
                IPBlockedList = new List<string> { "192.168.1.1-100" },
                ExcludeAllowedFromBlocked = true
            };

            return config;
        }

        private FileRoute GivenRoute(int port, string downstream, string upstream, FileSecurityOptions fileSecurityOptions = null)
            => new()
            {
                DownstreamPathTemplate = downstream,
                UpstreamPathTemplate = upstream,
                UpstreamHttpMethod = new List<string> { "Get" },
                SecurityOptions = fileSecurityOptions ?? new FileSecurityOptions(),
            };
    }
}
