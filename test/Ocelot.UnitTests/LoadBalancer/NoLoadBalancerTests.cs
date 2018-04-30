using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Middleware;
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
        private Response<ServiceHostAndPort> _result;

        [Fact]
        public void should_return_host_and_port()
        {
            var hostAndPort = new ServiceHostAndPort("127.0.0.1", 80);

            var services = new List<Service>
            {
                new Service("product", hostAndPort, string.Empty, string.Empty, new string[0])
            };
            this.Given(x => x.GivenServices(services))
                .When(x => x.WhenIGetTheNextHostAndPort())
                .Then(x => x.ThenTheHostAndPortIs(hostAndPort))
                .BDDfy();
        }

        [Fact]
        public void should_return_error_if_no_services()
        {
            var services = new List<Service>();

            this.Given(x => x.GivenServices(services))
                .When(x => x.WhenIGetTheNextHostAndPort())
                .Then(x => x.ThenThereIsAnError())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_if_null_services()
        {
            List<Service> services = null;

            this.Given(x => x.GivenServices(services))
                .When(x => x.WhenIGetTheNextHostAndPort())
                .Then(x => x.ThenThereIsAnError())
                .BDDfy();
        }

        private void ThenThereIsAnError()
        {
            _result.IsError.ShouldBeTrue();
        }

        private void GivenServices(List<Service> services)
        {
            _services = services;
        }

        private void WhenIGetTheNextHostAndPort()
        {
            _loadBalancer = new NoLoadBalancer(_services);
            _result = _loadBalancer.Lease(new DownstreamContext(new DefaultHttpContext())).Result;
        }

        private void ThenTheHostAndPortIs(ServiceHostAndPort expected)
        {
            _result.Data.ShouldBe(expected);
        }
    }
}
