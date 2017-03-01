using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.ServiceDiscovery;
using Shouldly;
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
                .WithServiceProviderConfiguraion(new ServiceProviderConfigurationBuilder().Build())
                .WithUpstreamHttpMethod("Get")
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
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
                .WithUpstreamHttpMethod("Get")
                .WithServiceProviderConfiguraion(new ServiceProviderConfigurationBuilder().Build())
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => x.GivenTheServiceProviderFactoryReturns())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<RoundRobinLoadBalancer>())
                .BDDfy();
        }

        [Fact]
        public void should_return_round_least_connection_balancer()
        {
             var reRoute = new ReRouteBuilder()
                .WithLoadBalancer("LeastConnection")
                .WithUpstreamHttpMethod("Get")
                .WithServiceProviderConfiguraion(new ServiceProviderConfigurationBuilder().Build())
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => x.GivenTheServiceProviderFactoryReturns())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<LeastConnectionLoadBalancer>())
                .BDDfy();
        }

        [Fact]
        public void should_call_service_provider()
        {
            var reRoute = new ReRouteBuilder()
                .WithLoadBalancer("RoundRobin")
                .WithUpstreamHttpMethod("Get")
                .WithServiceProviderConfiguraion(new ServiceProviderConfigurationBuilder().Build())
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => x.GivenTheServiceProviderFactoryReturns())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheServiceProviderIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheServiceProviderFactoryReturns()
        {
            _serviceProviderFactory
                .Setup(x => x.Get(It.IsAny<ServiceProviderConfiguration>()))
                .Returns(_serviceProvider.Object);
        }

        private void ThenTheServiceProviderIsCalledCorrectly()
        {
            _serviceProviderFactory
                .Verify(x => x.Get(It.IsAny<ServiceProviderConfiguration>()), Times.Once);
        }

        private void GivenAReRoute(ReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenIGetTheLoadBalancer()
        {
            _result = _factory.Get(_reRoute).Result;
        }

        private void ThenTheLoadBalancerIsReturned<T>()
        {
            _result.ShouldBeOfType<T>();
        }
    }
}