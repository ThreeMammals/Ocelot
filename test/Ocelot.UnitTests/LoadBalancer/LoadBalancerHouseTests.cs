﻿using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.UnitTests.LoadBalancer;

public class LoadBalancerHouseTests : UnitTest
{
    private DownstreamRoute _route;
    private ILoadBalancer _loadBalancer;
    private readonly LoadBalancerHouse _loadBalancerHouse;
    private Response<ILoadBalancer> _getResult;
    private readonly Mock<ILoadBalancerFactory> _factory;
    private readonly ServiceProviderConfiguration _serviceProviderConfig;

    public LoadBalancerHouseTests()
    {
        _factory = new Mock<ILoadBalancerFactory>();
        _loadBalancerHouse = new LoadBalancerHouse(_factory.Object);
        _serviceProviderConfig = new ServiceProviderConfiguration("myType", "myScheme", "myHost", 123, string.Empty, "configKey", 0);
    }

    [Fact]
    public void Should_store_load_balancer_on_first_request()
    {
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerKey("test")
            .Build();

        GivenThereIsALoadBalancer(route, new FakeLoadBalancer());
        ThenItIsAdded();
    }

    [Fact]
    public void Should_not_store_load_balancer_on_second_request()
    {
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions("FakeLoadBalancer", string.Empty, 0))
            .WithLoadBalancerKey("test")
            .Build();

        GivenThereIsALoadBalancer(route, new FakeLoadBalancer());
        WhenWeGetTheLoadBalancer(route);
        ThenItIsReturned();
    }

    [Fact]
    public void Should_store_load_balancers_by_key()
    {
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions("FakeLoadBalancer", string.Empty, 0))
            .WithLoadBalancerKey("test")
            .Build();

        var routeTwo = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions("FakeRoundRobinLoadBalancer", string.Empty, 0))
            .WithLoadBalancerKey("testtwo")
            .Build();

        GivenThereIsALoadBalancer(route, new FakeLoadBalancer());
        GivenThereIsALoadBalancer(routeTwo, new FakeRoundRobinLoadBalancer());
        WhenWeGetTheLoadBalancer(route);
        ThenTheLoadBalancerIs<FakeLoadBalancer>();
        WhenWeGetTheLoadBalancer(routeTwo);
        ThenTheLoadBalancerIs<FakeRoundRobinLoadBalancer>();
    }

    [Fact]
    public void Should_return_error_if_exception()
    {
        var route = new DownstreamRouteBuilder().Build();

        WhenWeGetTheLoadBalancer(route);
        ThenAnErrorIsReturned();
    }

    [Fact]
    public void Should_get_new_load_balancer_if_route_load_balancer_has_changed()
    {
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions("FakeLoadBalancer", string.Empty, 0))
            .WithLoadBalancerKey("test")
            .Build();

        var routeTwo = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions("LeastConnection", string.Empty, 0))
            .WithLoadBalancerKey("test")
            .Build();

        GivenThereIsALoadBalancer(route, new FakeLoadBalancer());
        WhenWeGetTheLoadBalancer(route);
        ThenTheLoadBalancerIs<FakeLoadBalancer>();
        WhenIGetTheRouteWithTheSameKeyButDifferentLoadBalancer(routeTwo);
        ThenTheLoadBalancerIs<LeastConnection>();
    }

    private void WhenIGetTheRouteWithTheSameKeyButDifferentLoadBalancer(DownstreamRoute route)
    {
        _route = route;
        _factory.Setup(x => x.Get(_route, _serviceProviderConfig)).Returns(new OkResponse<ILoadBalancer>(new LeastConnection(null, null)));
        _getResult = _loadBalancerHouse.Get(_route, _serviceProviderConfig);
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
        _factory.Verify(x => x.Get(_route, _serviceProviderConfig), Times.Once);
    }

    private void GivenThereIsALoadBalancer(DownstreamRoute route, ILoadBalancer loadBalancer)
    {
        _route = route;
        _loadBalancer = loadBalancer;
        _factory.Setup(x => x.Get(_route, _serviceProviderConfig)).Returns(new OkResponse<ILoadBalancer>(loadBalancer));
        _getResult = _loadBalancerHouse.Get(route, _serviceProviderConfig);
    }

    private void WhenWeGetTheLoadBalancer(DownstreamRoute route)
    {
        _getResult = _loadBalancerHouse.Get(route, _serviceProviderConfig);
    }

    private void ThenItIsReturned()
    {
        _getResult.Data.ShouldBe(_loadBalancer);
        _factory.Verify(x => x.Get(_route, _serviceProviderConfig), Times.Once);
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
