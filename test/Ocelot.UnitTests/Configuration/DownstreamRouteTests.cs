using Ocelot.Configuration;
using Ocelot.Configuration.Builder;

namespace Ocelot.UnitTests.Configuration;

[Collection(nameof(SequentialTests))]
public class DownstreamRouteTests
{
    [Fact]
    public void Name_UnknownPaths_ShouldBeQuestionMark()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(null)
            .WithDownstreamPathTemplate(null)
            .Build();

        // Act
        var actual = route.Name();

        // Assert
        Assert.Equal("?", actual);
    }

    [Theory]
    [InlineData(null, null, "?")]
    [InlineData(null, "NoServiceDiscoveryDownstreamPath", "NoServiceDiscoveryDownstreamPath")]
    [InlineData("NoServiceDiscoveryUpstreamPath", null, "NoServiceDiscoveryUpstreamPath")]
    public void Name_NoServiceDiscovery_ShouldBePathTemplate(string upstreamPathTemplate, string downstreamPathTemplate, string expectedName)
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new(upstreamPathTemplate, 0, false, upstreamPathTemplate))
            .WithDownstreamPathTemplate(downstreamPathTemplate)
            .Build();

        // Act
        var actual = route.Name();

        // Assert
        Assert.Equal(expectedName, actual);
    }

    [Theory]
    [InlineData(null, "?")]
    [InlineData("", "?")]
    [InlineData("TestTemplate", "TestTemplate")]
    public void Name_UpstreamPathTemplate_ShouldContainOriginalValue(string upstreamPathTemplate, string expectedName)
    {
        // Arrange
        var template = new UpstreamPathTemplateBuilder()
            .WithTemplate(upstreamPathTemplate)
            .WithOriginalValue(upstreamPathTemplate)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(template)
            .Build();

        // Act
        var actual = route.Name();

        // Assert
        Assert.Equal(expectedName, actual);
    }

    [Theory]
    [InlineData(null, "?")]
    [InlineData("", "?")]
    [InlineData("TestTemplate", "TestTemplate")]
    public void Name_DownstreamPathTemplate_ShouldContainPathTemplate(string downstreamPathTemplate, string expectedName)
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(null)
            .WithDownstreamPathTemplate(downstreamPathTemplate)
            .Build();

        // Act
        var actual = route.Name();

        // Assert
        Assert.Equal(expectedName, actual);
    }

    [Fact]
    public void Name_WithServiceDiscovery_ShouldBeUniqueDiscoveryString()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithServiceName("TestService")
            .WithServiceNamespace("TestNamespace")
            .WithUpstreamPathTemplate(new("/UpstreamPath", 0, false, "/UpstreamPath"))
            .Build();

        // Act
        var actual = route.Name();

        // Assert
        Assert.Equal("TestNamespace:TestService:/UpstreamPath", actual);
    }

    [Theory]
    [InlineData(false, "/test")]
    [InlineData(true, "TestNamespace:TestService:/test")]
    public void Name_UseServiceDiscovery_ShouldContainUpstreamPathTemplate(bool useServiceDiscovery, string expectedName)
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithUseServiceDiscovery(useServiceDiscovery)
            .WithUpstreamPathTemplate(new("/test", 0, false, "/test"))
            .WithServiceName(useServiceDiscovery ? "TestService" : string.Empty)
            .WithServiceNamespace("TestNamespace")
            .Build();

        // Act
        var actual = route.Name();

        // Assert
        Assert.Equal(expectedName, actual);
        Assert.Contains("/test", actual);
    }

    [Theory]
    [Trait("PR", "2073")]
    [InlineData(0, DownstreamRoute.DefTimeout)] // not in range
    [InlineData(DownstreamRoute.LowTimeout - 1, DownstreamRoute.DefTimeout)] // not in range
    [InlineData(DownstreamRoute.LowTimeout, DownstreamRoute.LowTimeout)] // in range
    [InlineData(DownstreamRoute.LowTimeout + 1, DownstreamRoute.LowTimeout + 1)] // in range
    [InlineData(DownstreamRoute.DefTimeout, DownstreamRoute.DefTimeout)] // in range
    public void DefaultTimeoutSeconds_Setter_ShouldBeGreaterThanOrEqualToThree(int value, int expected)
    {
        // Arrange, Act
        DownstreamRoute.DefaultTimeoutSeconds = value;

        // Assert
        Assert.Equal(expected, DownstreamRoute.DefaultTimeoutSeconds);
        DownstreamRoute.DefaultTimeoutSeconds = DownstreamRoute.DefTimeout; // recover clean state after assembly starting
    }

    [Fact]
    public void ToString_ShouldBeLoadBalancerKey()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerKey("testLbKey")
            .Build();

        // Act
        var actual = route.ToString();

        // Assert
        Assert.Equal("testLbKey", actual);
    }
}
