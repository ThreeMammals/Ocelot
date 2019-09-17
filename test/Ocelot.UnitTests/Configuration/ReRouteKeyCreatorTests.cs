using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class ReRouteKeyCreatorTests
    {
        private ReRouteKeyCreator _creator;
        private FileReRoute _reRoute;
        private string _result;

        public ReRouteKeyCreatorTests()
        {
            _creator = new ReRouteKeyCreator();
        }

        [Fact]
        public void should_return_sticky_session_key()
        {
            var reRoute = new FileReRoute
            {
                LoadBalancerOptions = new FileLoadBalancerOptions
                {
                    Key = "testy",
                    Type = nameof(CookieStickySessions)
                }
            };

            this.Given(_ => GivenThe(reRoute))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs($"{nameof(CookieStickySessions)}:{reRoute.LoadBalancerOptions.Key}"))
                .BDDfy();
        }

        [Fact]
        public void should_return_re_route_key()
        {
            var reRoute = new FileReRoute
            {
                UpstreamPathTemplate = "/api/product",
                UpstreamHttpMethod = new List<string> { "GET", "POST", "PUT" },
                DownstreamHostAndPorts = new List<FileHostAndPort>
                {
                    new FileHostAndPort
                    {
                        Host = "localhost",
                        Port = 123
                    },
                    new FileHostAndPort
                    {
                        Host = "localhost",
                        Port = 123
                    }
                }
            };

            this.Given(_ => GivenThe(reRoute))
                .When(_ => WhenICreate())
                .Then(_ => ThenTheResultIs($"{reRoute.UpstreamPathTemplate}|{string.Join(",", reRoute.UpstreamHttpMethod)}|{string.Join(",", reRoute.DownstreamHostAndPorts.Select(x => $"{x.Host}:{x.Port}"))}"))
                .BDDfy();
        }

        private void GivenThe(FileReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_reRoute);
        }

        private void ThenTheResultIs(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}
