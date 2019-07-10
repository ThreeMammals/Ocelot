using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ocelot.Middleware;
using Ocelot.Values;
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
        private readonly IEnumerable<ILoadBalancerCreator> _loadBalancerCreators;
        private readonly Mock<IServiceDiscoveryProvider> _serviceProvider;
        private ServiceProviderConfiguration _serviceProviderConfig;

        public LoadBalancerFactoryTests()
        {
            _serviceProviderFactory = new Mock<IServiceDiscoveryProviderFactory>();
            _serviceProvider = new Mock<IServiceDiscoveryProvider>();
            _loadBalancerCreators = new ILoadBalancerCreator[]
            {
                new FakeLoadBalancerCreator<FakeLoadBalancerOne>(),
                new FakeLoadBalancerCreator<FakeLoadBalancerTwo>(),
                new FakeLoadBalancerCreator<FakeNoLoadBalancer>(nameof(NoLoadBalancer)),
            };
            _factory = new LoadBalancerFactory(_serviceProviderFactory.Object, _loadBalancerCreators);
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
                .Then(x => x.ThenTheLoadBalancerIsReturned<FakeNoLoadBalancer>())
                .BDDfy();
        }

        [Fact]
        public void should_return_matching_load_balancer()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithLoadBalancerOptions(new LoadBalancerOptions("FakeLoadBalancerTwo", "", 0))
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => GivenAServiceProviderConfig(new ServiceProviderConfigurationBuilder().Build()))
                .And(x => x.GivenTheServiceProviderFactoryReturns())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheLoadBalancerIsReturned<FakeLoadBalancerTwo>())
                .BDDfy();
        }
        
        [Fact]
        public void should_call_service_provider()
        {
            var reRoute = new DownstreamReRouteBuilder()
                .WithLoadBalancerOptions(new LoadBalancerOptions("FakeLoadBalancerOne", "", 0))
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .And(x => GivenAServiceProviderConfig(new ServiceProviderConfigurationBuilder().Build()))
                .And(x => x.GivenTheServiceProviderFactoryReturns())
                .When(x => x.WhenIGetTheLoadBalancer())
                .Then(x => x.ThenTheServiceProviderIsCalledCorrectly())
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

        private class FakeLoadBalancerCreator<T> : ILoadBalancerCreator
            where T : ILoadBalancer, new()
        {

            public FakeLoadBalancerCreator()
            {
                Type = typeof(T).Name;
            }

            public FakeLoadBalancerCreator(string type)
            {
                Type = type;
            }

            public ILoadBalancer Create(DownstreamReRoute reRoute, IServiceDiscoveryProvider serviceProvider)
            {
                return new T();
            }
            
            public string Type { get; }
        }

        private class FakeLoadBalancerOne : ILoadBalancer
        {
            public Task<Response<ServiceHostAndPort>> Lease(DownstreamContext context)
            {
                throw new System.NotImplementedException();
            }

            public void Release(ServiceHostAndPort hostAndPort)
            {
                throw new System.NotImplementedException();
            }
        }

        private class FakeLoadBalancerTwo : ILoadBalancer
        {
            public Task<Response<ServiceHostAndPort>> Lease(DownstreamContext context)
            {
                throw new System.NotImplementedException();
            }

            public void Release(ServiceHostAndPort hostAndPort)
            {
                throw new System.NotImplementedException();
            }
        }

        private class FakeNoLoadBalancer : ILoadBalancer
        {
            public Task<Response<ServiceHostAndPort>> Lease(DownstreamContext context)
            {
                throw new System.NotImplementedException();
            }

            public void Release(ServiceHostAndPort hostAndPort)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
