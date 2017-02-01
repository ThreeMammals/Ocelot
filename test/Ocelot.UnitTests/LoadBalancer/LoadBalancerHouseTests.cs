using System;
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
        private ILoadBalancer _loadBalancer;
        private readonly LoadBalancerHouse _loadBalancerHouse;
        private Response _addResult;
        private Response<ILoadBalancer> _getResult;
        private string _key;

        public LoadBalancerHouseTests()
        {
            _loadBalancerHouse = new LoadBalancerHouse();
        }

        [Fact]
        public void should_store_load_balancer()
        {
            var key = "test";

            this.Given(x => x.GivenThereIsALoadBalancer(key, new FakeLoadBalancer()))
                .When(x => x.WhenIAddTheLoadBalancer())
                .Then(x => x.ThenItIsAdded())
                .BDDfy();
        }

        [Fact]
        public void should_get_load_balancer()
        {
            var key = "test";

            this.Given(x => x.GivenThereIsALoadBalancer(key, new FakeLoadBalancer()))
                .When(x => x.WhenWeGetThatLoadBalancer(key))
                .Then(x => x.ThenItIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_store_load_balancers_by_key()
        {
            var key = "test";
            var keyTwo = "testTwo";

            this.Given(x => x.GivenThereIsALoadBalancer(key, new FakeLoadBalancer()))
                .And(x => x.GivenThereIsALoadBalancer(keyTwo, new FakeRoundRobinLoadBalancer()))
                .When(x => x.WhenWeGetThatLoadBalancer(key))
                .Then(x => x.ThenTheLoadBalancerIs<FakeLoadBalancer>())
                .When(x => x.WhenWeGetThatLoadBalancer(keyTwo))
                .Then(x => x.ThenTheLoadBalancerIs<FakeRoundRobinLoadBalancer>())
                .BDDfy();
        }

        private void ThenTheLoadBalancerIs<T>()
        {
            _getResult.Data.ShouldBeOfType<T>();
        }

        private void ThenItIsAdded()
        {
            _addResult.IsError.ShouldBe(false);
            _addResult.ShouldBeOfType<OkResponse>();
        }

        private void WhenIAddTheLoadBalancer()
        {
            _addResult = _loadBalancerHouse.Add(_key, _loadBalancer);
        }


        private void GivenThereIsALoadBalancer(string key, ILoadBalancer loadBalancer)
        {
            _key = key;
            _loadBalancer = loadBalancer;
            WhenIAddTheLoadBalancer();
        }

        private void WhenWeGetThatLoadBalancer(string key)
        {
            _getResult = _loadBalancerHouse.Get(key);
        }

        private void ThenItIsReturned()
        {
            _getResult.Data.ShouldBe(_loadBalancer);
        }

        class FakeLoadBalancer : ILoadBalancer
        {
            public Response<HostAndPort> Lease()
            {
                throw new NotImplementedException();
            }

            public Response Release(HostAndPort hostAndPort)
            {
                throw new NotImplementedException();
            }
        }

        class FakeRoundRobinLoadBalancer : ILoadBalancer
        {
            public Response<HostAndPort> Lease()
            {
                throw new NotImplementedException();
            }

            public Response Release(HostAndPort hostAndPort)
            {
                throw new NotImplementedException();
            }
        }
    }
}
