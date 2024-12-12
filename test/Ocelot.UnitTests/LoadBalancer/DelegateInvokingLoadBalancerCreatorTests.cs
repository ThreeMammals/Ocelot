using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.UnitTests.LoadBalancer;

public class DelegateInvokingLoadBalancerCreatorTests : UnitTest
{
    private DelegateInvokingLoadBalancerCreator<FakeLoadBalancer> _creator;
    private Func<DownstreamRoute, IServiceDiscoveryProvider, ILoadBalancer> _creatorFunc;
    private readonly Mock<IServiceDiscoveryProvider> _serviceProvider;

    public DelegateInvokingLoadBalancerCreatorTests()
    {
        _creatorFunc = (route, serviceDiscoveryProvider) =>
            new FakeLoadBalancer(route, serviceDiscoveryProvider);
        _creator = new DelegateInvokingLoadBalancerCreator<FakeLoadBalancer>(_creatorFunc);
        _serviceProvider = new Mock<IServiceDiscoveryProvider>();
    }

    [Fact]
    public void Should_return_expected_name()
    {
        // Arrange
        var route = new DownstreamRouteBuilder().Build();

        // Act
        var loadBalancer = _creator.Create(route, _serviceProvider.Object);

        // Assert
        loadBalancer.Data.Type.ShouldBe(nameof(FakeLoadBalancer));
    }

    [Fact]
    public void Should_return_result_of_specified_creator_func()
    {
        // Arrange
        var route = new DownstreamRouteBuilder().Build();

        // Act
        var loadBalancer = _creator.Create(route, _serviceProvider.Object);

        // Assert
        loadBalancer.Data.ShouldBeOfType<FakeLoadBalancer>();
    }

    [Fact]
    public void Should_return_error()
    {
        // Arrange
        var route = new DownstreamRouteBuilder().Build();
        _creatorFunc = (route, serviceDiscoveryProvider) => throw new Exception();
        _creator = new DelegateInvokingLoadBalancerCreator<FakeLoadBalancer>(_creatorFunc);

        // Act
        var loadBalancer = _creator.Create(route, _serviceProvider.Object);

        // Assert
        loadBalancer.IsError.ShouldBeTrue();
    }

    private class FakeLoadBalancer : ILoadBalancer
    {
        public FakeLoadBalancer(DownstreamRoute downstreamRoute, IServiceDiscoveryProvider serviceDiscoveryProvider)
        {
            DownstreamRoute = downstreamRoute;
            ServiceDiscoveryProvider = serviceDiscoveryProvider;
        }

        public DownstreamRoute DownstreamRoute { get; }
        public IServiceDiscoveryProvider ServiceDiscoveryProvider { get; }
        public string Type => nameof(FakeLoadBalancer);
        public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext) => throw new NotImplementedException();
        public void Release(ServiceHostAndPort hostAndPort) => throw new NotImplementedException();
    }
}
