using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration.FileModels;

public class FileDynamicRouteTests
{
    [Fact]
    public void Ctor()
    {
        // Arrange, Act
        FileDynamicRoute instance = new();

        // Assert
        Assert.NotNull(instance.Metadata);
        Assert.Empty(instance.Metadata);
    }

    [Fact]
    public void Ctor_IRouteGrouping_IsImplemented()
    {
        // Arrange, Act
        FileDynamicRoute instance = new() { Key = "abc" };

        // Assert
        Assert.IsAssignableFrom<IRouteGrouping>(instance);
        IRouteGrouping obj = instance;
        Assert.Equal("abc", obj.Key);
    }

    [Fact]
    public void Ctor_IRouteUpstream_IsImplemented()
    {
        // Arrange, Act
        FileDynamicRoute instance = new()
        {
            ServiceName = "1",
            UpstreamHttpMethod = ["2"],
        };

        // Assert
        Assert.IsAssignableFrom<IRouteUpstream>(instance);
        IRouteUpstream obj = instance;
        Assert.NotNull(obj.UpstreamHeaderTemplates);
        Assert.Empty(obj.UpstreamHeaderTemplates);
        Assert.Equal(instance.ServiceName, obj.UpstreamPathTemplate);
        Assert.NotNull(obj.UpstreamHttpMethod);
        Assert.Contains("2", obj.UpstreamHttpMethod);
        Assert.False(obj.RouteIsCaseSensitive);
        Assert.Equal(0, obj.Priority);
    }

    [Fact]
    public void Ctor_IRouteRateLimiting_IsImplemented()
    {
        // Arrange
        FileRateLimitByHeaderRule rule = new() { ClientIdHeader = "111" };

        // Act
        FileDynamicRoute instance = new() { RateLimitOptions = rule };

        // Assert
        Assert.IsAssignableFrom<IRouteRateLimiting>(instance);
        IRouteRateLimiting obj = instance;
        Assert.Equal(rule, obj.RateLimitOptions);
        Assert.Equal("111", obj.RateLimitOptions.ClientIdHeader);
    }
}
