using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests
{
    public class LoadBalancerFactoryTests
    {
        private ReRoute _reRoute;
        private LoadBalancerFactory _factory;
        private ILoadBalancer _result;
        private Mock<Ocelot.ServiceDiscovery.IServiceProvider> _serviceProvider;
        
        public LoadBalancerFactoryTests()
        {
            _serviceProvider = new Mock<Ocelot.ServiceDiscovery.IServiceProvider>();
            _factory = new LoadBalancerFactory(_serviceProvider.Object);
        }

        [Fact]
        public void should_return_no_load_balancer()
        {
            var reRoute = new ReRouteBuilder()
            .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<NoLoadBalancer>())
                .BDDfy();
        }

        [Fact]
        public void should_return_round_robin_load_balancer()
        {
             var reRoute = new ReRouteBuilder()
                .WithLoadBalancer("RoundRobin")
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<RoundRobinLoadBalancer>())
                .BDDfy();
        }

        [Fact]
        public void should_return_round_least_connection_balancer()
        {
             var reRoute = new ReRouteBuilder()
                .WithLoadBalancer("LeastConnection")
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<LeastConnectionLoadBalancer>())
                .BDDfy();
        }

        [Fact]
        public void should_call_service_provider()
        {
            var reRoute = new ReRouteBuilder()
                .WithLoadBalancer("RoundRobin")
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheServiceProviderIsCalledCorrectly(reRoute))
                .BDDfy();
        }

        private void ThenTheServiceProviderIsCalledCorrectly(ReRoute reRoute)
        {
            _serviceProvider
                .Verify(x => x.Get(), Times.Once);
        }

        private void GivenAReRoute(ReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenIGetTheLoadBalancer()
        {
            _result = _factory.Get(_reRoute);
        }

        private void ThenTheLoadBalancerIsReturned<T>()
        {
            _result.ShouldBeOfType<T>();
        }
    }

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
                new Service("product", hostAndPort)
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
            _result = _loadBalancer.Lease();
        }

        private void ThenTheHostAndPortIs(HostAndPort expected)
        {
            _result.Data.ShouldBe(expected);
        }
    }

    public class NoLoadBalancer : ILoadBalancer
    {
        private List<Service> _services;

        public NoLoadBalancer(List<Service> services)
        {
            _services = services;
        }
        
        public Response<HostAndPort> Lease()
        {
            var service =  _services.FirstOrDefault();
            return new OkResponse<HostAndPort>(service.HostAndPort);
        }

        public Response Release(HostAndPort hostAndPort)
        {   
            return new OkResponse();
        }
    }

    public interface ILoadBalancerFactory
    {
        ILoadBalancer Get(ReRoute reRoute);
    }

    public class LoadBalancerFactory : ILoadBalancerFactory
    {
        private Ocelot.ServiceDiscovery.IServiceProvider _serviceProvider;

        public LoadBalancerFactory(Ocelot.ServiceDiscovery.IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ILoadBalancer Get(ReRoute reRoute)
        {
            switch (reRoute.LoadBalancer)
            {
                case "RoundRobin":
                    return new RoundRobinLoadBalancer(_serviceProvider.Get());
                case "LeastConnection":
                    return new LeastConnectionLoadBalancer(() => _serviceProvider.Get(), reRoute.ServiceName);
                default:
                    return new NoLoadBalancer(_serviceProvider.Get());
            }
        }
    }
}