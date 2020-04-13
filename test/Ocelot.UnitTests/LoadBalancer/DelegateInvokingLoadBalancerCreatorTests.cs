using System;
using System.Threading.Tasks;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class DelegateInvokingLoadBalancerCreatorTests
    {
        private DelegateInvokingLoadBalancerCreator<FakeLoadBalancer> _creator;
        private Func<DownstreamReRoute, IServiceDiscoveryProvider, ILoadBalancer> _creatorFunc;
        private readonly Mock<IServiceDiscoveryProvider> _serviceProvider;
        private DownstreamReRoute _reRoute;
        private Response<ILoadBalancer> _loadBalancer;
        private string _typeName;

        public DelegateInvokingLoadBalancerCreatorTests()
        {
            _creatorFunc = (reRoute, serviceDiscoveryProvider) =>
                new FakeLoadBalancer(reRoute, serviceDiscoveryProvider);
            _creator = new DelegateInvokingLoadBalancerCreator<FakeLoadBalancer>(_creatorFunc);
            _serviceProvider = new Mock<IServiceDiscoveryProvider>();
        }

        [Fact]
        public void should_return_expected_name()
        {
            this.When(x => x.WhenIGetTheLoadBalancerTypeName())
                .Then(x => x.ThenTheLoadBalancerTypeIs("FakeLoadBalancer"))
                .BDDfy();
        }

        [Fact]
        public void should_return_result_of_specified_creator_func()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<FakeLoadBalancer>())
                .BDDfy();
        }

        [Fact]
        public void should_return_error()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => x.GivenTheCreatorFuncThrows())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenAnErrorIsReturned())
                .BDDfy();
        }

        private void GivenTheCreatorFuncThrows()
        {
            _creatorFunc = (reRoute, serviceDiscoveryProvider) => throw new Exception();

            _creator = new DelegateInvokingLoadBalancerCreator<FakeLoadBalancer>(_creatorFunc);
        }

        private void ThenAnErrorIsReturned()
        {
            _loadBalancer.IsError.ShouldBeTrue();
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

        private class FakeLoadBalancer : ILoadBalancer
        {
            public FakeLoadBalancer(DownstreamReRoute reRoute, IServiceDiscoveryProvider serviceDiscoveryProvider)
            {
                ReRoute = reRoute;
                ServiceDiscoveryProvider = serviceDiscoveryProvider;
            }

            public DownstreamReRoute ReRoute { get; }
            public IServiceDiscoveryProvider ServiceDiscoveryProvider { get; }

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
