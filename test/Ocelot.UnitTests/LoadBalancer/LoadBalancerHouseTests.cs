using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.UnitTests.LoadBalancer;

public class LoadBalancerHouseTests : UnitTest
{
    private readonly LoadBalancerHouse _house;
    private readonly Mock<ILoadBalancerFactory> _factory;
    private readonly ServiceProviderConfiguration _serviceProviderConfig;

    public LoadBalancerHouseTests()
    {
        _factory = new Mock<ILoadBalancerFactory>();
        _house = new LoadBalancerHouse(_factory.Object);
        _serviceProviderConfig = new ServiceProviderConfiguration("myType", "myScheme", "myHost", 123, string.Empty, "configKey", 0);
    }

    [Fact]
    public void Should_store_load_balancer_on_first_request()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerKey("test")
            .Build();
        var loadBalancer = new FakeLoadBalancer();
        _factory.Setup(x => x.Get(route, _serviceProviderConfig)).Returns(new OkResponse<ILoadBalancer>(loadBalancer));

        // Act
        var result = _house.Get(route, _serviceProviderConfig);

        // Assert: Then It Is Added
        result.IsError.ShouldBe(false);
        result.ShouldBeOfType<OkResponse<ILoadBalancer>>();
        result.Data.ShouldBe(loadBalancer);
        _factory.Verify(x => x.Get(route, _serviceProviderConfig), Times.Once);
    }

    [Fact]
    public void Should_not_store_load_balancer_on_second_request()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions(nameof(FakeLoadBalancer), string.Empty, 0))
            .WithLoadBalancerKey("test")
            .Build();
        var loadBalancer = new FakeLoadBalancer();
        _factory.Setup(x => x.Get(route, _serviceProviderConfig)).Returns(new OkResponse<ILoadBalancer>(loadBalancer));

        // Act
        var result = _house.Get(route, _serviceProviderConfig);

        // Assert
        result.Data.ShouldBe(loadBalancer);
        _factory.Verify(x => x.Get(route, _serviceProviderConfig), Times.Once);
    }

    [Fact]
    public void Should_store_load_balancers_by_key()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions(nameof(FakeLoadBalancer), string.Empty, 0))
            .WithLoadBalancerKey("test")
            .Build();
        var route2 = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions(nameof(FakeRoundRobinLoadBalancer), string.Empty, 0))
            .WithLoadBalancerKey("testtwo")
            .Build();
        var loadBalancer = new FakeLoadBalancer();
        var loadBalancer2 = new FakeRoundRobinLoadBalancer();
        _factory.Setup(x => x.Get(route, _serviceProviderConfig)).Returns(new OkResponse<ILoadBalancer>(loadBalancer));
        _factory.Setup(x => x.Get(route2, _serviceProviderConfig)).Returns(new OkResponse<ILoadBalancer>(loadBalancer2));

        // Act, Assert
        var result = _house.Get(route, _serviceProviderConfig);
        result.Data.ShouldBeOfType<FakeLoadBalancer>();

        // Act, Assert
        result = _house.Get(route2, _serviceProviderConfig);
        result.Data.ShouldBeOfType<FakeRoundRobinLoadBalancer>();
    }

    [Fact]
    public void Should_return_error_if_exception()
    {
        // Arrange
        var route = new DownstreamRouteBuilder().Build();

        // Act
        var result = _house.Get(route, _serviceProviderConfig);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors[0].ShouldBeOfType<UnableToFindLoadBalancerError>();
    }

    [Fact]
    public void Should_get_new_load_balancer_if_route_load_balancer_has_changed()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions(nameof(FakeLoadBalancer), string.Empty, 0))
            .WithLoadBalancerKey("test")
            .Build();
        var route2 = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions(nameof(LeastConnection), string.Empty, 0))
            .WithLoadBalancerKey("test")
            .Build();
        var loadBalancer = new FakeLoadBalancer();
        _factory.Setup(x => x.Get(route, _serviceProviderConfig)).Returns(new OkResponse<ILoadBalancer>(loadBalancer));

        // Act, Assert
        var result = _house.Get(route, _serviceProviderConfig);
        result.Data.ShouldBeOfType<FakeLoadBalancer>();
        _factory.Setup(x => x.Get(route2, _serviceProviderConfig)).Returns(new OkResponse<ILoadBalancer>(new LeastConnection(null, null)));

        // Act, Assert
        result = _house.Get(route2, _serviceProviderConfig);
        result.Data.ShouldBeOfType<LeastConnection>();
    }

    private class FakeLoadBalancer : ILoadBalancer
    {
        public string Type => nameof(FakeLoadBalancer);
        public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext) => throw new NotImplementedException();
        public void Release(ServiceHostAndPort hostAndPort) => throw new NotImplementedException();
    }

    private class FakeRoundRobinLoadBalancer : ILoadBalancer
    {
        public string Type => nameof(FakeRoundRobinLoadBalancer);
        public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext) => throw new NotImplementedException();
        public void Release(ServiceHostAndPort hostAndPort) => throw new NotImplementedException();
    }
}
