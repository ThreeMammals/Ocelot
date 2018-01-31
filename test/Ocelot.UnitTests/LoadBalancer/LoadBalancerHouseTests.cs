using System;
using System.Threading.Tasks;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class LoadBalancerHouseTests
    {
        private ReRoute _reRoute;
        private ILoadBalancer _loadBalancer;
        private readonly LoadBalancerHouse _loadBalancerHouse;
        private Response _addResult;
        private Response<ILoadBalancer> _getResult;
        private string _key;
        private Mock<ILoadBalancerFactory> _factory;
        private ServiceProviderConfiguration _serviceProviderConfig;

        public LoadBalancerHouseTests()
        {
            _factory = new Mock<ILoadBalancerFactory>();
            _loadBalancerHouse = new LoadBalancerHouse(_factory.Object);
        }

        [Fact]
        public void should_store_load_balancer_on_first_request()
        {
            var reRoute = new ReRouteBuilder().WithReRouteKey("test").Build();

            this.Given(x => x.GivenThereIsALoadBalancer(reRoute, new FakeLoadBalancer()))
                .Then(x => x.ThenItIsAdded())
                .BDDfy();
        }

        [Fact]
        public void should_not_store_load_balancer_on_second_request()
        {
            var reRoute = new ReRouteBuilder().WithLoadBalancer("FakeLoadBalancer").WithReRouteKey("test").Build();

            this.Given(x => x.GivenThereIsALoadBalancer(reRoute, new FakeLoadBalancer()))
                .When(x => x.WhenWeGetTheLoadBalancer(reRoute))
                .Then(x => x.ThenItIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_store_load_balancers_by_key()
        {
            var reRoute = new ReRouteBuilder().WithReRouteKey("test").Build();
            var reRouteTwo = new ReRouteBuilder().WithReRouteKey("testtwo").Build();

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
            var reRoute = new ReRouteBuilder().Build();

            this.When(x => x.WhenWeGetTheLoadBalancer(reRoute))
            .Then(x => x.ThenAnErrorIsReturned())
            .BDDfy();
        }

        [Fact]
        public void should_get_new_load_balancer_if_reroute_load_balancer_has_changed()
        {
            var reRoute = new ReRouteBuilder().WithLoadBalancer("FakeLoadBalancer").WithReRouteKey("test").Build();

            var reRouteTwo = new ReRouteBuilder().WithLoadBalancer("LeastConnection").WithReRouteKey("test").Build();

            this.Given(x => x.GivenThereIsALoadBalancer(reRoute, new FakeLoadBalancer()))
                .When(x => x.WhenWeGetTheLoadBalancer(reRoute))
                .Then(x => x.ThenTheLoadBalancerIs<FakeLoadBalancer>())
                .When(x => x.WhenIGetTheReRouteWithTheSameKeyButDifferentLoadBalancer(reRouteTwo))
                .Then(x => x.ThenTheLoadBalancerIs<LeastConnection>())
                .BDDfy();
        }

        private void WhenIGetTheReRouteWithTheSameKeyButDifferentLoadBalancer(ReRoute reRoute)
        {
            _reRoute = reRoute;
            _factory.Setup(x => x.Get(_reRoute, _serviceProviderConfig)).ReturnsAsync(new LeastConnection(null, null));
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


        private void GivenThereIsALoadBalancer(ReRoute reRoute, ILoadBalancer loadBalancer)
        {
            _reRoute = reRoute;
            _loadBalancer = loadBalancer;
            _factory.Setup(x => x.Get(_reRoute, _serviceProviderConfig)).ReturnsAsync(loadBalancer);
            _getResult = _loadBalancerHouse.Get(reRoute, _serviceProviderConfig).Result;
        }

        private void WhenWeGetTheLoadBalancer(ReRoute reRoute)
        {
            _getResult = _loadBalancerHouse.Get(reRoute, _serviceProviderConfig).Result;
        }

        private void ThenItIsReturned()
        {
            _getResult.Data.ShouldBe(_loadBalancer);
            _factory.Verify(x => x.Get(_reRoute, _serviceProviderConfig), Times.Once);
        }

        class FakeLoadBalancer : ILoadBalancer
        {
            public Task<Response<ServiceHostAndPort>> Lease()
            {
                throw new NotImplementedException();
            }

            public void Release(ServiceHostAndPort hostAndPort)
            {
                throw new NotImplementedException();
            }
        }

        class FakeRoundRobinLoadBalancer : ILoadBalancer
        {
            public Task<Response<ServiceHostAndPort>> Lease()
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
