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
        private Mock<IServiceProvider> _serviceProvider;
        
        public LoadBalancerFactoryTests()
        {
            _serviceProvider = new Mock<IServiceProvider>();
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
                .Then(x => x.ThenTheServiceProviderIsCalledCorrectly())
                .BDDfy();
        }

        private void ThenTheServiceProviderIsCalledCorrectly()
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
            _result = _factory.Get(_reRoute.ServiceName, _reRoute.LoadBalancer);
        }

        private void ThenTheLoadBalancerIsReturned<T>()
        {
            _result.ShouldBeOfType<T>();
        }
    }
}