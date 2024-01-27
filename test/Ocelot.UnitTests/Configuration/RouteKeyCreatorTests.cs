using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;

namespace Ocelot.UnitTests.Configuration
{
    public class RouteKeyCreatorTests
    {
        private readonly RouteKeyCreator _creator;
        private FileRoute _route;
        private string _result;

        public RouteKeyCreatorTests()
        {
            _creator = new RouteKeyCreator();
        }

        [Fact]
        public void should_return_sticky_session_key()
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
        public void should_return_re_route_key()
        {
            var route = new FileRoute
            {
                UpstreamPathTemplate = "/api/product",
                UpstreamHttpMethod = ["GET", "POST", "PUT"],
                DownstreamHostAndPorts =
                [
                    new()
                    {
                        Host = "localhost",
                        Port = 8080,
                    },
                    new()
                    {
                        Host = "localhost",
                        Port = 4430,
                    },
                ],
            };

            this.Given(_ => GivenThe(route))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs("GET,POST,PUT|/api/product|||||"))
                .BDDfy();
        }

        [Fact]
        public void should_return_re_route_key_with_upstream_host()
        {
            var route = new FileRoute
            {
                UpstreamHost = "my-upstream-host",
                UpstreamPathTemplate = "/api/product",
                UpstreamHttpMethod = ["GET", "POST", "PUT"],
                DownstreamHostAndPorts =
                [
                    new()
                    {
                        Host = "localhost",
                        Port = 8080,
                    },
                    new()
                    {
                        Host = "localhost",
                        Port = 4430,
                    },
                ],
            };

            this.Given(_ => GivenThe(route))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs("GET,POST,PUT|/api/product|my-upstream-host||||"))
                .BDDfy();
        }

        [Fact]
        public void should_return_re_route_key_with_service_name()
        {
            var route = new FileRoute
            {
                UpstreamPathTemplate = "/api/product",
                UpstreamHttpMethod = ["GET", "POST", "PUT"],
                ServiceName = "my-service-name",
            };

            this.Given(_ => GivenThe(route))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs("GET,POST,PUT|/api/product|||my-service-name||"))
                .BDDfy();
        }

        [Fact]
        public void should_return_re_route_key_with_load_balancer_options()
        {
            var route = new FileRoute
            {
                UpstreamPathTemplate = "/api/product",
                UpstreamHttpMethod = ["GET", "POST", "PUT"],
                ServiceName = "my-service-name",
                LoadBalancerOptions = new FileLoadBalancerOptions
                {
                    Type = nameof(LeastConnection),
                    Key = "testy",
                },
            };

            this.Given(_ => GivenThe(route))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs("GET,POST,PUT|/api/product|||my-service-name|LeastConnection|testy"))
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
