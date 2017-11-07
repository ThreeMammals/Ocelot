using Xunit;
using Shouldly;
using TestStack.BDDfy;
using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.LoadBalancers;
using Moq;
using Ocelot.Configuration;
using System.Collections.Generic;
using Ocelot.Values;
using Ocelot.Configuration.Builder;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class LoadBalancerCreatorTests
    {
        private LoadBalancerCreator _creator;
        private ILoadBalancerHouse _house;
        private Mock<ILoadBalancerFactory> _factory;
        private ReRoute _reRoute;

        public LoadBalancerCreatorTests()
        {
            _house = new LoadBalancerHouse();
            _factory = new Mock<ILoadBalancerFactory>();
            _creator = new LoadBalancerCreator(_house, _factory.Object);
        }

        [Fact]
        public void should_create_load_balancer()
        {
            var reRoute = new ReRouteBuilder().WithLoadBalancerKey("Test").Build();
            this.Given(x => GivenTheFollowingReRoute(reRoute))
                .When(x => WhenICallTheCreator())
                .Then(x => x.ThenTheLoadBalancerIsCreated())
                .BDDfy();
        }

        private void GivenTheFollowingReRoute(ReRoute reRoute)
        {
            _reRoute = reRoute;
            _factory
                .Setup(x => x.Get(It.IsAny<ReRoute>()))
                .ReturnsAsync(new NoLoadBalancer(new List<Service>()));
        }

        private void WhenICallTheCreator()
        {
            _creator.SetupLoadBalancer(_reRoute).Wait();
        }

        private void ThenTheLoadBalancerIsCreated()
        {
            var lb = _house.Get(_reRoute.ReRouteKey);
            lb.ShouldNotBeNull();
        }
    }
}