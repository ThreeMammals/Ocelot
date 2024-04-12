using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;

namespace Ocelot.UnitTests.Configuration
{
    public class RouteKeyCreatorTests : UnitTest
    {
        private readonly RouteKeyCreator _creator;
        private FileRoute _route;
        private string _result;

        public RouteKeyCreatorTests()
        {
            _creator = new RouteKeyCreator();
        }

        [Fact]
        public void Should_return_sticky_session_key()
        {
            var route = new FileRoute
            {
                LoadBalancerOptions = new FileLoadBalancerOptions
                {
                    Key = "testy",
                    Type = nameof(CookieStickySessions),
                },
            };

            this.Given(_ => GivenThe(route))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs("CookieStickySessions:testy"))
                .BDDfy();
        }

        [Fact]
        public void Should_return_route_key()
        {
            var route = new FileRoute
            {
                UpstreamPathTemplate = "/api/product",
                UpstreamHttpMethod = new() { "GET", "POST", "PUT" },
                DownstreamHostAndPorts = new()
                {
                    new("localhost", 8080),
                    new("localhost", 4430),
                },
            };

            this.Given(_ => GivenThe(route))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs("GET,POST,PUT|/api/product|no-host|localhost:8080,localhost:4430|no-svc-ns|no-svc-name|no-lb-type|no-lb-key"))
                .BDDfy();
        }

        [Fact]
        public void Should_return_route_key_with_upstream_host()
        {
            var route = new FileRoute
            {
                UpstreamHost = "my-host",
                UpstreamPathTemplate = "/api/product",
                UpstreamHttpMethod = new() { "GET", "POST", "PUT" },
                DownstreamHostAndPorts = new()
                {
                    new("localhost", 8080),
                    new("localhost", 4430),
                },
            };

            this.Given(_ => GivenThe(route))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs("GET,POST,PUT|/api/product|my-host|localhost:8080,localhost:4430|no-svc-ns|no-svc-name|no-lb-type|no-lb-key"))
                .BDDfy();
        }

        [Fact]
        public void Should_return_route_key_with_svc_name()
        {
            var route = new FileRoute
            {
                UpstreamPathTemplate = "/api/product",
                UpstreamHttpMethod = new() { "GET", "POST", "PUT" },
                ServiceName = "products-service",
            };

            this.Given(_ => GivenThe(route))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs("GET,POST,PUT|/api/product|no-host|no-host-and-port|no-svc-ns|products-service|no-lb-type|no-lb-key"))
                .BDDfy();
        }

        [Fact]
        public void Should_return_route_key_with_load_balancer_options()
        {
            var route = new FileRoute
            {
                UpstreamPathTemplate = "/api/product",
                UpstreamHttpMethod = new() { "GET", "POST", "PUT" },
                ServiceName = "products-service",
                LoadBalancerOptions = new FileLoadBalancerOptions
                {
                    Type = nameof(LeastConnection),
                    Key = "testy",
                },
            };

            this.Given(_ => GivenThe(route))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs("GET,POST,PUT|/api/product|no-host|no-host-and-port|no-svc-ns|products-service|LeastConnection|testy"))
                .BDDfy();
        }

        private void GivenThe(FileRoute route)
        {
            _route = route;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_route);
        }

        private void ThenTheResultIs(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}
