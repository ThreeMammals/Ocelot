using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.Balancers;
using Shouldly;
using System.Reflection;

namespace Ocelot.UnitTests.LoadBalancer;

public class LoadBalancerOptionsCreatorTests : UnitTest
{
    private readonly LoadBalancerOptionsCreator _creator = new();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_create(bool isNull)
    {
        // Arrange
        var options = isNull ? null :
            new FileLoadBalancerOptions
            {
                Type = "test",
                Key = "west",
                Expiry = 1,
            };

        // Act
        var result = _creator.Create(options);

        // Assert
        result.Type.ShouldBe(isNull ? "NoLoadBalancer" : "test");
        result.Key.ShouldBe(isNull ? null : "west");
        result.ExpiryInMs.ShouldBe(isNull ? 0 : 1);
    }

    [Fact]
    [Trait("PR", "2324")]
    [Trait("Feat", "2319")]
    public void Create_FromRoute_NullChecks()
    {
        // Arrange, Act, Assert
        FileRoute route = null;
        FileGlobalConfiguration globalConfiguration = null;
        var actual = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(route), actual.ParamName);

        // Arrange, Act, Assert 2
        route = new();
        globalConfiguration = null;
        actual = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(globalConfiguration), actual.ParamName);
    }

    [Fact]
    [Trait("PR", "2324")]
    [Trait("Feat", "2319")]
    public void Create_FromRoute()
    {
        // Arrange
        FileRoute route = new()
        {
            LoadBalancerOptions = new()
            {
                Key = "route",
            },
        };
        FileGlobalConfiguration globalConfiguration = new()
        {
            LoadBalancerOptions = new("global"),
        };

        // Act
        var actual = _creator.Create(route, globalConfiguration);

        // Assert
        Assert.Equal("global", actual.Type);
        Assert.Equal("route", actual.Key);
        Assert.Equal(0, actual.ExpiryInMs);
    }

    [Fact]
    [Trait("PR", "2324")]
    [Trait("Feat", "2319")]
    public void Create_FromDynamicRoute_NullChecks()
    {
        // Arrange, Act, Assert
        FileDynamicRoute route = null;
        FileGlobalConfiguration globalConfiguration = null;
        var actual = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(route), actual.ParamName);

        // Arrange, Act, Assert 2
        route = new();
        globalConfiguration = null;
        actual = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(globalConfiguration), actual.ParamName);
    }

    [Fact]
    [Trait("PR", "2324")]
    [Trait("Feat", "2319")]
    public void Create_FromDynamicRoute()
    {
        // Arrange
        FileDynamicRoute route = new()
        {
            LoadBalancerOptions = new()
            {
                Key = "route",
            },
        };
        FileGlobalConfiguration globalConfiguration = new()
        {
            LoadBalancerOptions = new("global"),
        };

        // Act
        var actual = _creator.Create(route, globalConfiguration);

        // Assert
        Assert.Equal("global", actual.Type);
        Assert.Equal("route", actual.Key);
        Assert.Equal(0, actual.ExpiryInMs);
    }

    [Fact]
    [Trait("PR", "2324")]
    [Trait("Feat", "2319")]
    public void CreateProtected_NullCheck()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        IRouteGrouping grouping = null;
        FileLoadBalancerOptions options = null;
        FileGlobalLoadBalancerOptions globalOptions = null;

        // Act
        var wrapper = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(_creator, [grouping, options, globalOptions]));

        // Assert
        Assert.IsType<ArgumentNullException>(wrapper.InnerException);
        var actual = (ArgumentNullException)wrapper.InnerException;
        Assert.Equal(nameof(grouping), actual.ParamName);
    }

    [Fact]
    [Trait("PR", "2324")]
    [Trait("Feat", "2319")]
    public void CreateProtected()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        FileDynamicRoute route = new() { Key = "r1" };
        FileLoadBalancerOptions options = null;
        FileGlobalLoadBalancerOptions globalOptions = new()
        {
            RouteKeys = null,
            Type = "global",
            Key = "global",
            Expiry = 1,
        };

        // Act, Assert
        var actual = (LoadBalancerOptions)method.Invoke(_creator, [route, options, globalOptions]);
        Assert.Equal("global", actual.Type);
        Assert.Equal("global", actual.Key);
        Assert.Equal(1, actual.ExpiryInMs);

        // Arrange 2
        options = new()
        {
            Type = "route",
            Key = "route",
            Expiry = 3,
        };
        globalOptions.RouteKeys = ["?"];

        // Act, Assert 2
        actual = (LoadBalancerOptions)method.Invoke(_creator, [route, options, globalOptions]);
        Assert.Equal("route", actual.Type);
        Assert.Equal("route", actual.Key);
        Assert.Equal(3, actual.ExpiryInMs);

        globalOptions.RouteKeys = ["r1"];
        actual = (LoadBalancerOptions)method.Invoke(_creator, [route, options, globalOptions]);
        Assert.Equal("route", actual.Type);
        Assert.Equal("route", actual.Key);
        Assert.Equal(3, actual.ExpiryInMs);

        globalOptions = null;
        actual = (LoadBalancerOptions)method.Invoke(_creator, [route, options, globalOptions]);
        Assert.Equal("route", actual.Type);
        Assert.Equal("route", actual.Key);
        Assert.Equal(3, actual.ExpiryInMs);

        // Arrange 3
        options.Key = null;
        globalOptions = new()
        {
            RouteKeys = null,
            Type = "global",
            Key = "global",
            Expiry = 1,
        };
        actual = (LoadBalancerOptions)method.Invoke(_creator, [route, options, globalOptions]);
        Assert.Equal("route", actual.Type);
        Assert.Equal("global", actual.Key);
        Assert.Equal(3, actual.ExpiryInMs);
    }

    [Fact]
    [Trait("PR", "2324")]
    [Trait("Feat", "2319")]
    public void CreateProtected_NoOptions()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        FileDynamicRoute route = new();
        FileLoadBalancerOptions options = null;
        FileGlobalLoadBalancerOptions globalOptions = null;

        // Act
        var actual = (LoadBalancerOptions)method.Invoke(_creator, [route, options, globalOptions]);

        // Assert
        Assert.Equal(nameof(NoLoadBalancer), actual.Type);
    }

    [Fact]
    [Trait("PR", "2324")]
    [Trait("Feat", "2319")]

    public void Merge_NullCheck()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Merge", BindingFlags.Instance | BindingFlags.NonPublic);
        FileLoadBalancerOptions options = null;
        FileLoadBalancerOptions globalOptions = null;

        // Act, Assert 1
        var wrapper = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(_creator, [null, globalOptions]));
        Assert.IsType<ArgumentNullException>(wrapper.InnerException);
        var actual = (ArgumentNullException)wrapper.InnerException;
        Assert.Equal(nameof(options), actual.ParamName);

        // Act, Assert 2
        options = new();
        wrapper = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(_creator, [options, null]));
        Assert.IsType<ArgumentNullException>(wrapper.InnerException);
        actual = (ArgumentNullException)wrapper.InnerException;
        Assert.Equal(nameof(globalOptions), actual.ParamName);
    }

    [Theory]
    [Trait("PR", "2324")]
    [Trait("Feat", "2319")]
    [InlineData(false)]
    [InlineData(true)]
    public void Merge(bool isDef)
    {
        // Arrange
        var method = _creator.GetType().GetMethod(nameof(Merge), BindingFlags.Instance | BindingFlags.NonPublic);
        FileLoadBalancerOptions options = new()
        {
            Type = isDef ? string.Empty : "route",
            Key = isDef ? string.Empty : "route",
            Expiry = isDef ? null : 1,
        };
        FileLoadBalancerOptions globalOptions = new("global")
        {
            Key = "global",
            Expiry = 3,
        };

        // Act
        var actual = (LoadBalancerOptions)method.Invoke(_creator, [options, globalOptions]);

        // Assert
        Assert.Equal(isDef ? "global" : "route", actual.Type);
        Assert.Equal(isDef ? "global" : "route", actual.Key);
        Assert.Equal(isDef ? 3 : 1, actual.ExpiryInMs);
    }
}
