using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Infrastructure.RequestData;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.UnitTests.LoadBalancer;

public class LoadBalancerFactoryTests : UnitTest
{
    private readonly LoadBalancerFactory _factory;
    private readonly Mock<IServiceDiscoveryProviderFactory> _serviceProviderFactory;
    private readonly IEnumerable<ILoadBalancerCreator> _loadBalancerCreators;
    private readonly Mock<IServiceDiscoveryProvider> _serviceProvider;

    public LoadBalancerFactoryTests()
    {
        _serviceProviderFactory = new Mock<IServiceDiscoveryProviderFactory>();
        _serviceProvider = new Mock<IServiceDiscoveryProvider>();
        _loadBalancerCreators = new ILoadBalancerCreator[]
        {
            new FakeLoadBalancerCreator<FakeLoadBalancerOne>(),
            new FakeLoadBalancerCreator<FakeLoadBalancerTwo>(),
            new FakeLoadBalancerCreator<FakeNoLoadBalancer>(nameof(NoLoadBalancer)),
            new BrokenLoadBalancerCreator<BrokenLoadBalancer>(),
        };
        _factory = new LoadBalancerFactory(_serviceProviderFactory.Object, _loadBalancerCreators);
    }

    [Fact]
    public void Should_return_no_load_balancer_by_default()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithUpstreamHttpMethod(new() { HttpMethods.Get })
            .Build();
        var config = new ServiceProviderConfigurationBuilder().Build();
        GivenTheServiceProviderFactoryReturns();

        // Act
        var result = _factory.Get(route, config);

        // Assert
        result.Data.ShouldBeOfType<FakeNoLoadBalancer>();
    }

    [Fact]
    public void Should_return_matching_load_balancer()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions(nameof(FakeLoadBalancerTwo), string.Empty, 0))
            .WithUpstreamHttpMethod(new() { HttpMethods.Get })
            .Build();
        var config = new ServiceProviderConfigurationBuilder().Build();
        GivenTheServiceProviderFactoryReturns();

        // Act
        var result = _factory.Get(route, config);

        // Assert
        result.Data.ShouldBeOfType<FakeLoadBalancerTwo>();
    }

    [Fact]
    public void Should_return_error_response_if_cannot_find_load_balancer_creator()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions("DoesntExistLoadBalancer", string.Empty, 0))
            .WithUpstreamHttpMethod(new() { HttpMethods.Get })
            .Build();
        var config = new ServiceProviderConfigurationBuilder().Build();
        GivenTheServiceProviderFactoryReturns();

        // Act
        var result = _factory.Get(route, config);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors[0].Message.ShouldBe("Could not find load balancer creator for Type: DoesntExistLoadBalancer, please check your config specified the correct load balancer and that you have registered a class with the same name.");
    }

    [Fact]
    public void Should_return_error_response_if_creator_errors()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions(nameof(BrokenLoadBalancer), string.Empty, 0))
            .WithUpstreamHttpMethod(new() { HttpMethods.Get })
            .Build();
        var config = new ServiceProviderConfigurationBuilder().Build();
        GivenTheServiceProviderFactoryReturns();

        // Act
        var result = _factory.Get(route, config);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void Should_call_service_provider()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions(nameof(FakeLoadBalancerOne), string.Empty, 0))
            .WithUpstreamHttpMethod(new() { HttpMethods.Get })
            .Build();
        var config = new ServiceProviderConfigurationBuilder().Build();
        GivenTheServiceProviderFactoryReturns();

        // Act
        var result = _factory.Get(route, config);

        // Assert
        ThenTheServiceProviderIsCalledCorrectly();
    }

    [Fact]
    public void Should_return_error_response_when_call_to_service_provider_fails()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions(nameof(FakeLoadBalancerOne), string.Empty, 0))
            .WithUpstreamHttpMethod(new() { HttpMethods.Get })
            .Build();
        var config = new ServiceProviderConfigurationBuilder().Build();
        GivenTheServiceProviderFactoryFails();

        // Act
        var result = _factory.Get(route, config);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    private void GivenTheServiceProviderFactoryReturns()
    {
        _serviceProviderFactory
            .Setup(x => x.Get(It.IsAny<ServiceProviderConfiguration>(), It.IsAny<DownstreamRoute>()))
            .Returns(new OkResponse<IServiceDiscoveryProvider>(_serviceProvider.Object));
    }

    private void GivenTheServiceProviderFactoryFails()
    {
        _serviceProviderFactory
            .Setup(x => x.Get(It.IsAny<ServiceProviderConfiguration>(), It.IsAny<DownstreamRoute>()))
            .Returns(new ErrorResponse<IServiceDiscoveryProvider>(new CannotFindDataError("For tests")));
    }

    private void ThenTheServiceProviderIsCalledCorrectly()
    {
        _serviceProviderFactory
            .Verify(x => x.Get(It.IsAny<ServiceProviderConfiguration>(), It.IsAny<DownstreamRoute>()), Times.Once);
    }

    private class FakeLoadBalancerCreator<T> : ILoadBalancerCreator
        where T : ILoadBalancer, new()
    {
        public FakeLoadBalancerCreator() => Type = typeof(T).Name;
        public FakeLoadBalancerCreator(string type) => Type = type;
        public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider) => new OkResponse<ILoadBalancer>(new T());
        public string Type { get; }
    }

    private class BrokenLoadBalancerCreator<T> : ILoadBalancerCreator
        where T : ILoadBalancer, new()
    {
        public BrokenLoadBalancerCreator() => Type = typeof(T).Name;
        public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
            => new ErrorResponse<ILoadBalancer>(new InvokingLoadBalancerCreatorError(new Exception()));
        public string Type { get; }
    }

    private class FakeLoadBalancerOne : ILoadBalancer
    {
        public string Type => nameof(FakeLoadBalancerOne);
        public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext) => throw new NotImplementedException();
        public void Release(ServiceHostAndPort hostAndPort) => throw new NotImplementedException();
    }

    private class FakeLoadBalancerTwo : ILoadBalancer
    {
        public string Type => nameof(FakeLoadBalancerTwo);
        public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext) => throw new NotImplementedException();
        public void Release(ServiceHostAndPort hostAndPort) => throw new NotImplementedException();
    }

    private class FakeNoLoadBalancer : ILoadBalancer
    {
        public string Type => nameof(FakeNoLoadBalancer);
        public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext) => throw new NotImplementedException();
        public void Release(ServiceHostAndPort hostAndPort) => throw new NotImplementedException();
    }

    private class BrokenLoadBalancer : ILoadBalancer
    {
        public string Type => nameof(BrokenLoadBalancer);
        public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext) => throw new NotImplementedException();
        public void Release(ServiceHostAndPort hostAndPort) => throw new NotImplementedException();
    }
}
