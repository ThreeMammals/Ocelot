using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.UnitTests.LoadBalancer;

public class NoLoadBalancerTests : UnitTest
{
    private readonly List<Service> _services;
    private NoLoadBalancer _loadBalancer;
    private Response<ServiceHostAndPort> _result;

    public NoLoadBalancerTests()
    {
        _services = new List<Service>();
        _loadBalancer = new NoLoadBalancer(() => Task.FromResult(_services));
    }

    [Fact]
    public async Task Should_return_host_and_port()
    {
        // Arrange
        var hostAndPort = new ServiceHostAndPort("127.0.0.1", 80);
        var services = new List<Service>
        {
            new("product", hostAndPort, string.Empty, string.Empty, Array.Empty<string>()),
        };
        _services.AddRange(services);

        // Act
        _result = await _loadBalancer.LeaseAsync(new DefaultHttpContext());

        // Assert
        _result.Data.ShouldBe(hostAndPort);
    }

    [Fact]
    public async Task Should_return_error_if_no_services()
    {
        // Arrange, Act
        _result = await _loadBalancer.LeaseAsync(new DefaultHttpContext());

        // Assert
        _result.IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_return_error_if_no_services_then_when_services_available_return_host_and_port()
    {
        // Arrange
        var hostAndPort = new ServiceHostAndPort("127.0.0.1", 80);

        var services = new List<Service>
        {
            new("product", hostAndPort, string.Empty, string.Empty, Array.Empty<string>()),
        };

        // Act, Assert
        _result = await _loadBalancer.LeaseAsync(new DefaultHttpContext());
        _result.IsError.ShouldBeTrue();
        _services.AddRange(services);

        // Act, Assert
        _result = await _loadBalancer.LeaseAsync(new DefaultHttpContext());
        _result.Data.ShouldBe(hostAndPort);
    }

    [Fact]
    public async Task Should_return_error_if_null_services()
    {
        // Arrange
        _loadBalancer = new NoLoadBalancer(() => Task.FromResult((List<Service>)null));

        // Act
        _result = await _loadBalancer.LeaseAsync(new DefaultHttpContext());

        // Assert
        _result.IsError.ShouldBeTrue();
    }
}
