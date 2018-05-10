using Ocelot.DownstreamRouteFinder.Finder;
using Xunit;
using Shouldly;
using Ocelot.Configuration;
using System.Net.Http;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    public class DownstreamRouteCreatorTests
    {
        private DownstreamRouteCreator _creator;

        public DownstreamRouteCreatorTests()
        {
            _creator = new DownstreamRouteCreator();
        }

        [Fact]
        public void should_create_downstream_route()
        {
            var upstreamUrlPath = "/auth/test";
            var upstreamHttpMethod = "GET";
            IInternalConfiguration configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter");
            var upstreamHost = "doesnt matter";
            var result = _creator.Get(upstreamUrlPath, upstreamHttpMethod, configuration, upstreamHost);
            result.Data.ReRoute.DownstreamReRoute[0].DownstreamPathTemplate.Value.ShouldBe("/test");
            result.Data.ReRoute.UpstreamHttpMethod[0].ShouldBe(HttpMethod.Get);
            result.Data.ReRoute.DownstreamReRoute[0].ServiceName.ShouldBe("auth");
            result.Data.ReRoute.DownstreamReRoute[0].LoadBalancerKey.ShouldBe("/auth/test|GET");
        }

        [Fact]
        public void should_create_downstream_route_and_remove_query_string()
        {
            var upstreamUrlPath = "/auth/test?test=1&best=2";
            var upstreamHttpMethod = "GET";
            IInternalConfiguration configuration = new InternalConfiguration(null, "doesnt matter", null, "doesnt matter");
            var upstreamHost = "doesnt matter";
            var result = _creator.Get(upstreamUrlPath, upstreamHttpMethod, configuration, upstreamHost);
            result.Data.ReRoute.DownstreamReRoute[0].DownstreamPathTemplate.Value.ShouldBe("/test");
            result.Data.ReRoute.UpstreamHttpMethod[0].ShouldBe(HttpMethod.Get);
            result.Data.ReRoute.DownstreamReRoute[0].ServiceName.ShouldBe("auth");
            result.Data.ReRoute.DownstreamReRoute[0].LoadBalancerKey.ShouldBe("/auth/test?test=1&best=2|GET");
        }
    }
}
