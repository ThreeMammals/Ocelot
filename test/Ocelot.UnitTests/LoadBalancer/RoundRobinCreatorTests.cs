namespace Ocelot.UnitTests.LoadBalancer
{
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.LoadBalancer.LoadBalancers;
    using Ocelot.Responses;
    using Ocelot.ServiceDiscovery.Providers;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class RoundRobinCreatorTests
    {
        private readonly RoundRobinCreator _creator;
        private readonly Mock<IServiceDiscoveryProvider> _serviceProvider;
        private DownstreamReRoute _reRoute;
        private Response<ILoadBalancer> _loadBalancer;
        private string _typeName;

        public RoundRobinCreatorTests()
        {
            _creator = new RoundRobinCreator();
            _serviceProvider = new Mock<IServiceDiscoveryProvider>();
        }
        
        [Fact]
        public void should_return_instance_of_expected_load_balancer_type()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<RoundRobin>())
                .BDDfy();
        }
                
        [Fact]
        public void should_return_expected_name()
        {
            this.When(x => x.WhenIGetTheLoadBalancerTypeName())
                .Then(x => x.ThenTheLoadBalancerTypeIs("RoundRobin"))
                .BDDfy();
        }

        private void GivenAReRoute(DownstreamReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenIGetTheLoadBalancer()
        {
            _loadBalancer = _creator.Create(_reRoute, _serviceProvider.Object);
        }
        
        private void WhenIGetTheLoadBalancerTypeName()
        {
            _typeName = _creator.Type;
        }

        private void ThenTheLoadBalancerIsReturned<T>()
            where T : ILoadBalancer
        {
            _loadBalancer.Data.ShouldBeOfType<T>();
        }

        private void ThenTheLoadBalancerTypeIs(string type)
        {
            _typeName.ShouldBe(type);
        }
    }
}
