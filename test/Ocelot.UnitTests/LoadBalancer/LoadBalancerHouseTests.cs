using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using System;
using System.Threading.Tasks;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class LoadBalancerHouseTests
    {
        private DownstreamReRoute _reRoute;
        private ILoadBalancer _loadBalancer;
        private readonly LoadBalancerHouse _loadBalancerHouse;
        private Response<ILoadBalancer> _getResult;
        private readonly Mock<ILoadBalancerFactory> _factory;
        private readonly ServiceProviderConfiguration _serviceProviderConfig;

        public LoadBalancerHouseTests()
        {
            _factory = new Mock<ILoadBalancerFactory>();
            _loadBalancerHouse = new LoadBalancerHouse(_factory.Object);
            _serviceProviderConfig = new ServiceProviderConfiguration("myType", "myHost", 123, string.Empty, "configKey", 0);
        }

        [Fact]
        public void should_store_load_balancer_on_first_request()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithLoadBalancerKey("test")
                .Build();

            this.Given(x => x.GivenThereIsALoadBalancer(reRoute, new FakeLoadBalancer()))
                .Then(x => x.ThenItIsAdded())
                .BDDfy();
        }

        [Fact]
        public void should_not_store_load_balancer_on_second_request()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithLoadBalancerOptions(new LoadBalancerOptions("FakeLoadBalancer", "", 0))
                .WithLoadBalancerKey("test")
                .Build();

            this.Given(x => x.GivenThereIsALoadBalancer(reRoute, new FakeLoadBalancer()))
                .When(x => x.WhenWeGetTheLoadBalancer(reRoute))
                .Then(x => x.ThenItIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_store_load_balancers_by_key()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithLoadBalancerOptions(new LoadBalancerOptions("FakeLoadBalancer", "", 0))
                .WithLoadBalancerKey("test")
                .Build();

            var reRouteTwo = new DownstreamReRouteBuilder()
                .WithLoadBalancerOptions(new LoadBalancerOptions("FakeRoundRobinLoadBalancer", "", 0))
                .WithLoadBalancerKey("testtwo")
                .Build();

            this.Given(x => x.GivenThereIsALoadBalancer(reRoute, new FakeLoadBalancer()))
                .And(x => x.GivenThereIsALoadBalancer(reRouteTwo, new FakeRoundRobinLoadBalancer()))
                .When(x => x.WhenWeGetTheLoadBalancer(reRoute))
                .Then(x => x.ThenTheLoadBalancerIs<FakeLoadBalancer>())
                .When(x => x.WhenWeGetTheLoadBalancer(reRouteTwo))
                .Then(x => x.ThenTheLoadBalancerIs<FakeRoundRobinLoadBalancer>())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_if_exception()
        {
            var reRoute = new DownstreamReRouteBuilder().Build();

            this.When(x => x.WhenWeGetTheLoadBalancer(reRoute))
            .Then(x => x.ThenAnErrorIsReturned())
            .BDDfy();
        }

        [Fact]
        public void should_get_new_load_balancer_if_reroute_load_balancer_has_changed()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithLoadBalancerOptions(new LoadBalancerOptions("FakeLoadBalancer", "", 0))
                .WithLoadBalancerKey("test")
                .Build();

            var reRouteTwo = new DownstreamReRouteBuilder()
                .WithLoadBalancerOptions(new LoadBalancerOptions("LeastConnection", "", 0))
                .WithLoadBalancerKey("test")
                .Build();

            this.Given(x => x.GivenThereIsALoadBalancer(reRoute, new FakeLoadBalancer()))
                .When(x => x.WhenWeGetTheLoadBalancer(reRoute))
                .Then(x => x.ThenTheLoadBalancerIs<FakeLoadBalancer>())
                .When(x => x.WhenIGetTheReRouteWithTheSameKeyButDifferentLoadBalancer(reRouteTwo))
                .Then(x => x.ThenTheLoadBalancerIs<LeastConnection>())
                .BDDfy();
        }

        private void WhenIGetTheReRouteWithTheSameKeyButDifferentLoadBalancer(DownstreamReRoute reRoute)
        {
            _reRoute = reRoute;
            _factory.Setup(x => x.Get(_reRoute, _serviceProviderConfig)).ReturnsAsync(new OkResponse<ILoadBalancer>(new LeastConnection(null, null)));
            _getResult = _loadBalancerHouse.Get(_reRoute, _serviceProviderConfig).Result;
        }

        private void ThenAnErrorIsReturned()
        {
            _getResult.IsError.ShouldBeTrue();
            _getResult.Errors[0].ShouldBeOfType<UnableToFindLoadBalancerError>();
        }

        private void ThenTheLoadBalancerIs<T>()
        {
            _getResult.Data.ShouldBeOfType<T>();
        }

        private void ThenItIsAdded()
        {
            _getResult.IsError.ShouldBe(false);
            _getResult.ShouldBeOfType<OkResponse<ILoadBalancer>>();
            _getResult.Data.ShouldBe(_loadBalancer);
            _factory.Verify(x => x.Get(_reRoute, _serviceProviderConfig), Times.Once);
        }

        private void GivenThereIsALoadBalancer(DownstreamReRoute reRoute, ILoadBalancer loadBalancer)
        {
            _reRoute = reRoute;
            _loadBalancer = loadBalancer;
            _factory.Setup(x => x.Get(_reRoute, _serviceProviderConfig)).ReturnsAsync(new OkResponse<ILoadBalancer>(loadBalancer));
            _getResult = _loadBalancerHouse.Get(reRoute, _serviceProviderConfig).Result;
        }

        private void WhenWeGetTheLoadBalancer(DownstreamReRoute reRoute)
        {
            _getResult = _loadBalancerHouse.Get(reRoute, _serviceProviderConfig).Result;
        }

        private void ThenItIsReturned()
        {
            _getResult.Data.ShouldBe(_loadBalancer);
            _factory.Verify(x => x.Get(_reRoute, _serviceProviderConfig), Times.Once);
        }

        private class FakeLoadBalancer : ILoadBalancer
        {
            public Task<Response<ServiceHostAndPort>> Lease(DownstreamContext context)
            {
                throw new NotImplementedException();
            }

            public void Release(ServiceHostAndPort hostAndPort)
            {
                throw new NotImplementedException();
            }
        }

        private class FakeRoundRobinLoadBalancer : ILoadBalancer
        {
            public Task<Response<ServiceHostAndPort>> Lease(DownstreamContext context)
            {
                throw new NotImplementedException();
            }

            public void Release(ServiceHostAndPort hostAndPort)
            {
                throw new NotImplementedException();
            }
        }
    }
}
