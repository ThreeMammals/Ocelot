using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.ServiceDiscovery;
using Shouldly;
using System.Collections.Generic;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class LoadBalancerFactoryTests
    {
        private ReRoute _reRoute;
        private LoadBalancerFactory _factory;
        private ILoadBalancer _result;
        private Mock<IServiceDiscoveryProviderFactory> _serviceProviderFactory;
        private Mock<IServiceDiscoveryProvider> _serviceProvider;
        private ServiceProviderConfiguration _serviceProviderConfig;

        public LoadBalancerFactoryTests()
        {
            _serviceProviderFactory = new Mock<IServiceDiscoveryProviderFactory>();
            _serviceProvider = new Mock<IServiceDiscoveryProvider>();
            _factory = new LoadBalancerFactory(_serviceProviderFactory.Object);
        }

        [Fact]
        public void should_return_no_load_balancer()
        {
            var reRoute = new ReRouteBuilder()
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => GivenAServiceProviderConfig(new ServiceProviderConfigurationBuilder().Build()))
                .And(x => x.GivenTheServiceProviderFactoryReturns())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<NoLoadBalancer>())
                .BDDfy();
        }

        [Fact]
        public void should_return_round_robin_load_balancer()
        {
             var reRoute = new ReRouteBuilder()
                .WithLoadBalancer("RoundRobin")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => GivenAServiceProviderConfig(new ServiceProviderConfigurationBuilder().Build()))
                .And(x => x.GivenTheServiceProviderFactoryReturns())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<RoundRobin>())
                .BDDfy();
        }

        [Fact]
        public void should_return_round_least_connection_balancer()
        {
             var reRoute = new ReRouteBuilder()
                .WithLoadBalancer("LeastConnection")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => GivenAServiceProviderConfig(new ServiceProviderConfigurationBuilder().Build()))
                .And(x => x.GivenTheServiceProviderFactoryReturns())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<LeastConnection>())
                .BDDfy();
        }

        [Fact]
        public void should_call_service_provider()
        {
            var reRoute = new ReRouteBuilder()
                .WithLoadBalancer("RoundRobin")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => GivenAServiceProviderConfig(new ServiceProviderConfigurationBuilder().Build()))
                .And(x => x.GivenTheServiceProviderFactoryReturns())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheServiceProviderIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenAServiceProviderConfig(ServiceProviderConfiguration serviceProviderConfig)
        {
            _serviceProviderConfig = serviceProviderConfig;
        }

        private void GivenTheServiceProviderFactoryReturns()
        {
            _serviceProviderFactory
                .Setup(x => x.Get(It.IsAny<ServiceProviderConfiguration>(), It.IsAny<ReRoute>()))
                .Returns(_serviceProvider.Object);
        }

        private void ThenTheServiceProviderIsCalledCorrectly()
        {
            _serviceProviderFactory
                .Verify(x => x.Get(It.IsAny<ServiceProviderConfiguration>(), It.IsAny<ReRoute>()), Times.Once);
        }

        private void GivenAReRoute(ReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenIGetTheLoadBalancer()
        {
            _result = _factory.Get(_reRoute, _serviceProviderConfig).Result;
        }

        private void ThenTheLoadBalancerIsReturned<T>()
        {
            _result.ShouldBeOfType<T>();
        }
    }
}