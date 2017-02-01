using System;
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

        public LoadBalancerFactoryTests()
        {
            _factory = new LoadBalancerFactory();
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

    public class NoLoadBalancer : ILoadBalancer
    {
        Response<HostAndPort> ILoadBalancer.Lease()
        {
            throw new NotImplementedException();
        }

        Response ILoadBalancer.Release(HostAndPort hostAndPort)
        {
            throw new NotImplementedException();
        }
    }

    public interface ILoadBalancerFactory
    {
        ILoadBalancer Get(ReRoute reRoute);
    }

    public class LoadBalancerFactory : ILoadBalancerFactory
    {
        public ILoadBalancer Get(ReRoute reRoute)
        {
            switch (reRoute.LoadBalancer)
            {
                case "RoundRobin":
                    return new RoundRobinLoadBalancer(null);
                default:
                    return new NoLoadBalancer();
            }
        }
    }
}