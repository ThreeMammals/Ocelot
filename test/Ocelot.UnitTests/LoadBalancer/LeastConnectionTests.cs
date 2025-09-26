using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Values;

namespace Ocelot.UnitTests.LoadBalancer;

public class LeastConnectionTests : UnitTest
{
    private LeastConnection _leastConnection;
    private readonly Random _random;
    private readonly DefaultHttpContext _httpContext;

    public LeastConnectionTests()
    {
        _httpContext = new();
        _random = new();
    }

    [Fact]
    public async Task Should_be_able_to_lease_and_release_concurrently()
    {
        const string ServiceName = "products";
        var availableServices = new List<Service>
        {
            new(ServiceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
            new(ServiceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
        };
        _leastConnection = new LeastConnection(() => Task.FromResult(availableServices), ServiceName);

        var tasks = new Task[100];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = LeaseDelayAndRelease();
        }

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Should_handle_service_returning_to_available()
    {
        const string ServiceName = "products";

        var availableServices = new List<Service>
        {
            new(ServiceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
            new(ServiceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
        };

        _leastConnection = new LeastConnection(() => Task.FromResult(availableServices), ServiceName);

        var hostAndPortOne = await _leastConnection.LeaseAsync(_httpContext);
        hostAndPortOne.Data.DownstreamHost.ShouldBe("127.0.0.1");
        var hostAndPortTwo = await _leastConnection.LeaseAsync(_httpContext);
        hostAndPortTwo.Data.DownstreamHost.ShouldBe("127.0.0.2");
        _leastConnection.Release(hostAndPortOne.Data);
        _leastConnection.Release(hostAndPortTwo.Data);

        availableServices = new List<Service>
        {
            new(ServiceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
        };

        hostAndPortOne = await _leastConnection.LeaseAsync(_httpContext);
        hostAndPortOne.Data.DownstreamHost.ShouldBe("127.0.0.1");
        hostAndPortTwo = await _leastConnection.LeaseAsync(_httpContext);
        hostAndPortTwo.Data.DownstreamHost.ShouldBe("127.0.0.1");
        _leastConnection.Release(hostAndPortOne.Data);
        _leastConnection.Release(hostAndPortTwo.Data);

        availableServices = new List<Service>
        {
            new(ServiceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
            new(ServiceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
        };

        hostAndPortOne = await _leastConnection.LeaseAsync(_httpContext);
        hostAndPortOne.Data.DownstreamHost.ShouldBe("127.0.0.1");
        hostAndPortTwo = await _leastConnection.LeaseAsync(_httpContext);
        hostAndPortTwo.Data.DownstreamHost.ShouldBe("127.0.0.2");
        _leastConnection.Release(hostAndPortOne.Data);
        _leastConnection.Release(hostAndPortTwo.Data);
    }

    private async Task LeaseDelayAndRelease()
    {
        var hostAndPort = await _leastConnection.LeaseAsync(_httpContext);
        await Task.Delay(_random.Next(1, 100));
        _leastConnection.Release(hostAndPort.Data);
    }

    [Fact]
    public async Task Should_get_next_url()
    {
        // Arrange
        const string ServiceName = "products";
        var hostAndPort = new ServiceHostAndPort("localhost", 80);
        var availableServices = new List<Service>
        {
            new(ServiceName, hostAndPort, string.Empty, string.Empty, Array.Empty<string>()),
        };
        _leastConnection = new LeastConnection(() => Task.FromResult(availableServices), ServiceName);

        // Act
        var result = await _leastConnection.LeaseAsync(_httpContext);

        // Assert
        result.Data.DownstreamHost.ShouldBe(hostAndPort.DownstreamHost);
        result.Data.DownstreamPort.ShouldBe(hostAndPort.DownstreamPort);
    }

    [Fact]
    public async Task Should_serve_from_service_with_least_connections()
    {
        const string ServiceName = "products";
        var availableServices = new List<Service>
        {
            new(ServiceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
            new(ServiceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
            new(ServiceName, new ServiceHostAndPort("127.0.0.3", 80), string.Empty, string.Empty, Array.Empty<string>()),
        };

        _leastConnection = new LeastConnection(() => Task.FromResult(availableServices), ServiceName);

        var response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

        response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

        response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[2].HostAndPort.DownstreamHost);
    }

    [Fact]
    public async Task Should_build_connections_per_service()
    {
        const string ServiceName = "products";
        var availableServices = new List<Service>
        {
            new(ServiceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
            new(ServiceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
        };

        _leastConnection = new LeastConnection(() => Task.FromResult(availableServices), ServiceName);

        var response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

        response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

        response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

        response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);
    }

    [Fact]
    public async Task Should_release_connection()
    {
        const string ServiceName = "products";
        var availableServices = new List<Service>
        {
            new(ServiceName, new ServiceHostAndPort("127.0.0.1", 80), string.Empty, string.Empty, Array.Empty<string>()),
            new(ServiceName, new ServiceHostAndPort("127.0.0.2", 80), string.Empty, string.Empty, Array.Empty<string>()),
        };

        _leastConnection = new LeastConnection(() => Task.FromResult(availableServices), ServiceName);

        var response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

        response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

        response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[0].HostAndPort.DownstreamHost);

        response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);

        //release this so 2 should have 1 connection and we should get 2 back as our next host and port
        _leastConnection.Release(availableServices[1].HostAndPort);

        response = await _leastConnection.LeaseAsync(_httpContext);

        response.Data.DownstreamHost.ShouldBe(availableServices[1].HostAndPort.DownstreamHost);
    }

    [Fact]
    public async Task Should_return_error_if_services_are_null()
    {
        // Arrange
        const string ServiceName = "products";
        var hostAndPort = new ServiceHostAndPort("localhost", 80);
        _leastConnection = new LeastConnection(() => Task.FromResult((List<Service>)null), ServiceName);

        // Act
        var result = await _leastConnection.LeaseAsync(_httpContext);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors[0].ShouldBeOfType<ServicesAreNullError>();
    }

    [Fact]
    public async Task Should_return_error_if_services_are_empty()
    {
        // Arrange
        const string ServiceName = "products";
        var hostAndPort = new ServiceHostAndPort("localhost", 80);
        _leastConnection = new LeastConnection(() => Task.FromResult(new List<Service>()), ServiceName);

        // Act
        var result = await _leastConnection.LeaseAsync(_httpContext);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors[0].ShouldBeOfType<ServicesAreNullError>();
    }
}
