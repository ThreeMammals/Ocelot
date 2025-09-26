using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.Balancers;

namespace Ocelot.UnitTests.Configuration;

public class RouteKeyCreatorTests : UnitTest
{
    private readonly RouteKeyCreator _creator = new();

    [Fact]
    public void Should_return_sticky_session_key()
    {
        // Arrange
        var route = new FileRoute
        {
            LoadBalancerOptions = new FileLoadBalancerOptions
            {
                Key = "testy",
                Type = nameof(CookieStickySessions),
            },
        };

        // Act
        var result = _creator.Create(route);

        // Assert
        result.ShouldBe("CookieStickySessions:testy");
    }

    [Fact]
    public void Should_return_route_key()
    {
        // Arrange
        var route = new FileRoute
        {
            UpstreamPathTemplate = "/api/product",
            UpstreamHttpMethod = ["GET", "POST", "PUT"],
            DownstreamHostAndPorts = new()
            {
                new("localhost", 8080),
                new("localhost", 4430),
            },
        };

        // Act
        var result = _creator.Create(route);

        // Assert
        result.ShouldBe("GET,POST,PUT|/api/product|no-host|localhost:8080,localhost:4430|no-svc-ns|no-svc-name|no-lb-type|no-lb-key");
    }

    [Fact]
    public void Should_return_route_key_with_upstream_host()
    {
        // Arrange
        var route = new FileRoute
        {
            UpstreamHost = "my-host",
            UpstreamPathTemplate = "/api/product",
            UpstreamHttpMethod = ["GET", "POST", "PUT"],
            DownstreamHostAndPorts = new()
            {
                new("localhost", 8080),
                new("localhost", 4430),
            },
        };

        // Act
        var result = _creator.Create(route);

        // Assert
        result.ShouldBe("GET,POST,PUT|/api/product|my-host|localhost:8080,localhost:4430|no-svc-ns|no-svc-name|no-lb-type|no-lb-key");
    }

    [Fact]
    public void Should_return_route_key_with_svc_name()
    {
        // Arrange
        var route = new FileRoute
        {
            UpstreamPathTemplate = "/api/product",
            UpstreamHttpMethod = ["GET", "POST", "PUT"],
            ServiceName = "products-service",
        };

        // Act
        var result = _creator.Create(route);

        // Assert
        result.ShouldBe("GET,POST,PUT|/api/product|no-host|no-host-and-port|no-svc-ns|products-service|no-lb-type|no-lb-key");
    }

    [Fact]
    public void Should_return_route_key_with_load_balancer_options()
    {
        // Arrange
        var route = new FileRoute
        {
            UpstreamPathTemplate = "/api/product",
            UpstreamHttpMethod = ["GET", "POST", "PUT"],
            ServiceName = "products-service",
            LoadBalancerOptions = new FileLoadBalancerOptions
            {
                Type = nameof(LeastConnection),
                Key = "testy",
            },
        };

        // Act
        var result = _creator.Create(route);

        // Assert
        result.ShouldBe("GET,POST,PUT|/api/product|no-host|no-host-and-port|no-svc-ns|products-service|LeastConnection|testy");
    }
}
