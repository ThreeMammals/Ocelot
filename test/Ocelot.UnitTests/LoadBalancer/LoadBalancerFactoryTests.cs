using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using Shouldly;
using System.Collections.Generic;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class LoadBalancerFactoryTests
    {
        private DownstreamReRoute _reRoute;
        private readonly LoadBalancerFactory _factory;
        private Response<ILoadBalancer> _result;
        private readonly Mock<IServiceDiscoveryProviderFactory> _serviceProviderFactory;
        private readonly Mock<IServiceDiscoveryProvider> _serviceProvider;
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
            var reRoute = new DownstreamReRouteBuilder()
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
            var reRoute = new DownstreamReRouteBuilder()
                .WithLoadBalancerOptions(new LoadBalancerOptions("RoundRobin", "", 0))
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
            var reRoute = new DownstreamReRouteBuilder()
                .WithLoadBalancerOptions(new LoadBalancerOptions("LeastConnection", "", 0))
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
            var reRoute = new DownstreamReRouteBuilder()
                .WithLoadBalancerOptions(new LoadBalancerOptions("RoundRobin", "", 0))
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => GivenAServiceProviderConfig(new ServiceProviderConfigurationBuilder().Build()))
                .And(x => x.GivenTheServiceProviderFactoryReturns())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheServiceProviderIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_return_sticky_session()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithLoadBalancerOptions(new LoadBalancerOptions("CookieStickySessions", "", 0))
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => GivenAServiceProviderConfig(new ServiceProviderConfigurationBuilder().Build()))
                .And(x => x.GivenTheServiceProviderFactoryReturns())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<CookieStickySessions>())
                .BDDfy();
        }

        private void GivenAServiceProviderConfig(ServiceProviderConfiguration serviceProviderConfig)
        {
            _serviceProviderConfig = serviceProviderConfig;
        }

        private void GivenTheServiceProviderFactoryReturns()
        {
            _serviceProviderFactory
                .Setup(x => x.Get(It.IsAny<ServiceProviderConfiguration>(), It.IsAny<DownstreamReRoute>()))
                .Returns(new OkResponse<IServiceDiscoveryProvider>(_serviceProvider.Object));
        }

        private void ThenTheServiceProviderIsCalledCorrectly()
        {
            _serviceProviderFactory
                .Verify(x => x.Get(It.IsAny<ServiceProviderConfiguration>(), It.IsAny<DownstreamReRoute>()), Times.Once);
        }

        private void GivenAReRoute(DownstreamReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenIGetTheLoadBalancer()
        {
            _result = _factory.Get(_reRoute, _serviceProviderConfig).Result;
        }

        private void ThenTheLoadBalancerIsReturned<T>()
        {
            _result.Data.ShouldBeOfType<T>();
        }
    }
}
