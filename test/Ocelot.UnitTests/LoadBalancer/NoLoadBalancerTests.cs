using System.Collections.Generic;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class NoLoadBalancerTests
    {
        private List<Service> _services;
        private NoLoadBalancer _loadBalancer;
        private Response<HostAndPort> _result;

        [Fact]
        public void should_return_host_and_port()
        {
            var hostAndPort = new HostAndPort("127.0.0.1", 80);

            var services = new List<Service>
            {
                new Service("product", hostAndPort, string.Empty, string.Empty, new string[0])
            };
            this.Given(x => x.GivenServices(services))
                .When(x => x.WhenIGetTheNextHostAndPort())
                .Then(x => x.ThenTheHostAndPortIs(hostAndPort))
                .BDDfy();
        }

        private void GivenServices(List<Service> services)
        {
            _services = services;
        }

        private void WhenIGetTheNextHostAndPort()
        {
            _loadBalancer = new NoLoadBalancer(_services);
            _result = _loadBalancer.Lease().Result;
        }

        private void ThenTheHostAndPortIs(HostAndPort expected)
        {
            _result.Data.ShouldBe(expected);
        }
    }
}