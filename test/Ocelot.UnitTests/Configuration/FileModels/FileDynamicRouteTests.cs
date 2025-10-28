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
        Assert.Null(instance.Metadata);
        Assert.Null(instance.Key);
        Assert.Null(instance.RateLimitRule);
        Assert.Null(instance.RateLimitOptions);
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
