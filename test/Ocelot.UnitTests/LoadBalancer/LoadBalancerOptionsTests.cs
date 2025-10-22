using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.Balancers;

namespace Ocelot.UnitTests.LoadBalancer;

public class LoadBalancerOptionsTests
{
    [Fact]
    public void Ctor_ShouldDefaultToNoLoadBalancer()
    {
        // Arrange, Act
        LoadBalancerOptions options = new();
        LoadBalancerOptions options2 = new(default, default, default);
        LoadBalancerOptions options3 = new(new FileLoadBalancerOptions());

        // Assert
        Assert.Equal(nameof(NoLoadBalancer), options.Type);
        Assert.Equal(nameof(NoLoadBalancer), options2.Type);
        Assert.Equal(nameof(NoLoadBalancer), options3.Type);
    }

    [Fact]
    public void Ctor_Parameterless()
    {
        // Arrange, Act
        LoadBalancerOptions actual = new();

        // Assert
        Assert.Equal(nameof(NoLoadBalancer), actual.Type);
        Assert.Null(actual.Key);
        Assert.Equal(0, actual.ExpiryInMs);
    }

    [Fact]
    [Trait("PR", "2324")]
    public void Ctor_CopyingFrom_FileLoadBalancerOptions()
    {
        // Arrange
        FileLoadBalancerOptions from = new()
        {
            Type = "Balancer",
            Key = "BalancerKey",
            Expiry = 3,
        };

        // Act
        LoadBalancerOptions actual = new(from);

        // Assert
        Assert.False(ReferenceEquals(from, actual));
        Assert.Equal("Balancer", actual.Type);
        Assert.Equal("BalancerKey", actual.Key);
        Assert.Equal(3, actual.ExpiryInMs);
    }

    [Theory]
    [Trait("PR", "2324")]
    [InlineData(false)]
    [InlineData(true)]
    public void Ctor_Initialization3Params(bool isDef)
    {
        // Arrange
        FileLoadBalancerOptions from = new()
        {
            Type = isDef ? string.Empty : "TestBalancer",
            Key = isDef ? string.Empty : "Bla-Bla",
            Expiry = isDef ? null : 3,
        };

        // Act
        LoadBalancerOptions actual = new(from.Type, from.Key, from.Expiry);

        // Assert
        Assert.Equal(isDef ? nameof(NoLoadBalancer) : "TestBalancer", actual.Type);
        Assert.Equal(isDef ? string.Empty : "Bla-Bla", actual.Key);
        Assert.Equal(isDef ? 0 : 3, actual.ExpiryInMs);
    }

    [Theory]
    [Trait("PR", "2324")]
    [InlineData(false)]
    [InlineData(true)]
    public void Ctor_Initialization_CookieStickySessions(bool isDef)
    {
        // Arrange
        FileLoadBalancerOptions from = new()
        {
            Type = nameof(CookieStickySessions),
            Key = isDef ? string.Empty : "Key",
            Expiry = isDef ? null : 3,
        };

        // Act
        LoadBalancerOptions actual = new(from.Type, from.Key, from.Expiry);

        // Assert
        Assert.Equal("CookieStickySessions", actual.Type);
        Assert.Equal(isDef ? ".AspNetCore.Session" : "Key", actual.Key);
        Assert.Equal(isDef ? 1200000 : 3, actual.ExpiryInMs);
    }
}
