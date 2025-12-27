using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using System.Reflection;

namespace Ocelot.UnitTests.Cache;

public class CacheOptionsCreatorTests : UnitTest
{
    private readonly CacheOptionsCreator _creator = new();

    [Fact]
    public void Should_create_region_from_loadBalancingKey()
    {
        // Arrange
        var route = new FileRoute
        {
            FileCacheOptions = new()
            {
                Region = string.Empty,
            },
        };

        // Act
        var actual = _creator.Create(route, new FileGlobalConfiguration(), "testKey");

        // Assert
        Assert.Equal("testKey", actual.Region);
    }

    [Fact]
    public void Should_use_region()
    {
        // Arrange
        var route = new FileRoute
        {
            FileCacheOptions = new()
            {
                Region = "region",
            },
        };

        // Act
        var actual = _creator.Create(route, new FileGlobalConfiguration(), "bla-bla");

        // Assert
        Assert.Equal("region", actual.Region);
    }

    [Fact]
    [Trait("Feat", "2058")]
    [Trait("Bug", "2059")]
    public void ShouldCreateCacheOptions()
    {
        // Arrange
        var options = GivenCacheOptions();
        var route = GivenRoute(options);

        // Act
        var result = _creator.Create(route, new(), null);

        // Assert
        result.TtlSeconds.ShouldBe(options.TtlSeconds.Value);
        result.Region.ShouldBe(options.Region);
        result.Header.ShouldBe(options.Header);
        result.EnableContentHashing.ShouldBe(options.EnableContentHashing.Value);
    }

    [Fact]
    [Trait("Feat", "2058")]
    [Trait("Bug", "2059")]
    public void ShouldCreateCacheOptionsUsingGlobalConfiguration()
    {
        // Arrange
        var global = GivenGlobalConfiguration();
        var options = new FileCacheOptions();
        var route = GivenRoute(options);

        // Act
        var result = _creator.Create(route, global, null);

        // Assert
        result.TtlSeconds.ShouldBe(global.CacheOptions.TtlSeconds.Value);
        result.Region.ShouldBe(global.CacheOptions.Region);
        result.Header.ShouldBe(global.CacheOptions.Header);
        result.EnableContentHashing.ShouldBe(global.CacheOptions.EnableContentHashing.Value);
    }

    [Fact]
    [Trait("Feat", "2058")]
    [Trait("Bug", "2059")]
    public void RouteCacheOptionsShouldOverrideGlobalConfiguration()
    {
        // Arrange
        var global = GivenGlobalConfiguration();
        var options = GivenCacheOptions();
        var route = GivenRoute(options);

        // Act
        var result = _creator.Create(route, global, null);

        // Assert
        result.TtlSeconds.ShouldBe(options.TtlSeconds.Value);
        result.Region.ShouldBe(options.Region);
        result.Header.ShouldBe(options.Header);
        result.EnableContentHashing.ShouldBe(options.EnableContentHashing.Value);
    }

    [Fact]
    [Trait("Feat", "2058")]
    [Trait("Bug", "2059")]
    public void ShouldCreateCacheOptionsWithDefaults()
    {
        // Arrange
        var options = new FileCacheOptions();
        var route = GivenRoute(options);

        // Act
        var result = _creator.Create(route, new(), "testLbKey");

        // Assert
        result.TtlSeconds.ShouldBe(0);
        result.Region.ShouldBe("testLbKey");
        result.Header.ShouldBe("OC-Cache-Control");
        result.EnableContentHashing.ShouldBe(false);
    }

    [Fact]
    [Trait("Feat", "2058")]
    [Trait("Bug", "2059")]
    public void ShouldComputeRegionIfNotProvided()
    {
        // Arrange
        var global = GivenGlobalConfiguration();
        var options = GivenCacheOptions();
        var route = GivenRoute(options);
        global.CacheOptions.Region = null;
        options.Region = null;

        // Act
        var result = _creator.Create(route, global, "testLbKey");

        // Assert
        result.TtlSeconds.ShouldBe(options.TtlSeconds.Value);
        result.Region.ShouldBe("testLbKey");
        result.Header.ShouldBe(options.Header);
        result.EnableContentHashing.ShouldBe(options.EnableContentHashing.Value);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2330")] // https://github.com/ThreeMammals/Ocelot/issues/2330
    public void Create_FileCacheOptions()
    {
        // Arrange, Act, Assert : null
        FileCacheOptions options = null;
        var actual = _creator.Create(options);
        Assert.NotNull(actual);
        Assert.False(actual.UseCache);

        // Arrange, Act, Assert : not null
        options = GivenCacheOptions();
        actual = _creator.Create(options);
        Assert.Equal(options.TtlSeconds.Value, actual.TtlSeconds);
        Assert.Equal(options.Region, actual.Region);
        Assert.Equal(options.Header, actual.Header);
        Assert.Equal(options.EnableContentHashing.Value, actual.EnableContentHashing);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2330")] // https://github.com/ThreeMammals/Ocelot/issues/2330
    public void Create_FromRoute_NullChecks()
    {
        // Arrange, Act, Assert
        FileRoute route = null;
        FileGlobalConfiguration globalConfiguration = null;
        var actual = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration, "lbKey"));
        Assert.Equal(nameof(route), actual.ParamName);

        // Arrange, Act, Assert 2
        route = new();
        globalConfiguration = null;
        actual = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration, "lbKey"));
        Assert.Equal(nameof(globalConfiguration), actual.ParamName);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2330")] // https://github.com/ThreeMammals/Ocelot/issues/2330
    public void Create_FromRoute()
    {
        // Arrange
        FileRoute route = new()
        {
            FileCacheOptions = new()
            {
                TtlSeconds = 1,
                Region = "route",
            },
        };
        FileGlobalConfiguration globalConfiguration = new()
        {
            CacheOptions = new()
            {
                TtlSeconds = 33,
                Region = "global",
            },
        };

        // Act, Assert : from FileCacheOptions
        var actual = _creator.Create(route, globalConfiguration, "lbKey");
        Assert.Equal(1, actual.TtlSeconds);
        Assert.Equal("route", actual.Region);
        Assert.Equal("OC-Cache-Control", actual.Header);
        Assert.False(actual.EnableContentHashing);

        // Arrange : from CacheOptions
        route.FileCacheOptions = null;
        route.CacheOptions = new()
        {
            TtlSeconds = 2,
            Region = "route",
            Header = "fromCacheOptions",
            EnableContentHashing = true,
        };

        // Act, Assert : from CacheOptions
        actual = _creator.Create(route, globalConfiguration, "lbKey");
        Assert.Equal(2, actual.TtlSeconds);
        Assert.Equal("route", actual.Region);
        Assert.Equal("fromCacheOptions", actual.Header);
        Assert.True(actual.EnableContentHashing);

        // Arrange, Act, Assert : from route if not in the group
        route.Key = "bla-bla";
        globalConfiguration.CacheOptions.RouteKeys = ["R1"];
        actual = _creator.Create(route, globalConfiguration, "lbKey");
        Assert.Equal(2, actual.TtlSeconds);
        Assert.Equal("route", actual.Region);

        // Arrange, Act, Assert : from global
        route.CacheOptions = null;
        globalConfiguration.CacheOptions.RouteKeys.Clear();
        actual = _creator.Create(route, globalConfiguration, "lbKey");
        Assert.Equal(33, actual.TtlSeconds);
        Assert.Equal("global", actual.Region);
        Assert.Equal("OC-Cache-Control", actual.Header);
        Assert.False(actual.EnableContentHashing);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2330")] // https://github.com/ThreeMammals/Ocelot/issues/2330
    public void Create_FromDynamicRoute_NullChecks()
    {
        // Arrange, Act, Assert
        FileDynamicRoute route = null;
        FileGlobalConfiguration globalConfiguration = null;
        var actual = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration, "lbKey"));
        Assert.Equal(nameof(route), actual.ParamName);

        // Arrange, Act, Assert 2
        route = new();
        globalConfiguration = null;
        actual = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration, "lbKey"));
        Assert.Equal(nameof(globalConfiguration), actual.ParamName);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2330")] // https://github.com/ThreeMammals/Ocelot/issues/2330
    public void Create_FromDynamicRoute()
    {
        // Arrange
        FileDynamicRoute route = new()
        {
            CacheOptions = new()
            {
                TtlSeconds = 1,
                Region = "route",
                Header = "route",
                EnableContentHashing = true,
            },
        };
        FileGlobalConfiguration globalConfiguration = new()
        {
            CacheOptions = new()
            {
                TtlSeconds = 2,
                Region = "global",
                Header = "global",
            },
        };

        // Act
        var actual = _creator.Create(route, globalConfiguration, "lbKey");

        // Assert
        Assert.Equal(1, actual.TtlSeconds);
        Assert.Equal("route", actual.Region);
        Assert.Equal("route", actual.Header);
        Assert.True(actual.EnableContentHashing);

        // Arrange, Act, Assert : from global
        route.CacheOptions = null;
        actual = _creator.Create(route, globalConfiguration, "lbKey");
        Assert.Equal(2, actual.TtlSeconds);
        Assert.Equal("global", actual.Region);
        Assert.Equal("global", actual.Header);
        Assert.False(actual.EnableContentHashing);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2330")] // https://github.com/ThreeMammals/Ocelot/issues/2330
    public void CreateProtected()
    {
        // Scenario 1: Null checks
        // Arrange
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        IRouteGrouping grouping = null;
        FileCacheOptions options = null;
        FileGlobalCacheOptions globalOptions = null;
        string loadBalancingKey = "lbKey";

        // Act
        var wrapper = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(_creator, [grouping, options, globalOptions, loadBalancingKey]));

        // Assert : Null checks
        Assert.IsType<ArgumentNullException>(wrapper.InnerException);
        var actualEx = (ArgumentNullException)wrapper.InnerException;
        Assert.Equal(nameof(grouping), actualEx.ParamName);

        // Scenario 2: if-else branches
        FileDynamicRoute route = new() { Key = "r1" };
        options = null;
        globalOptions = new()
        {
            RouteKeys = null,
            Region = "global",
            Header = "global",
            TtlSeconds = 3,
        };

        // Act, Assert
        var actual = (CacheOptions)method.Invoke(_creator, [route, options, globalOptions, loadBalancingKey]);
        Assert.Equal("global", actual.Region);
        Assert.Equal("global", actual.Header);
        Assert.Equal(3, actual.TtlSeconds);

        // Arrange 2
        options = new()
        {
            Region = "route",
            Header = "route",
            TtlSeconds = 1,
        };
        globalOptions.RouteKeys = ["?"];

        // Act, Assert 2
        actual = (CacheOptions)method.Invoke(_creator, [route, options, globalOptions, loadBalancingKey]);
        Assert.Equal("route", actual.Region);
        Assert.Equal("route", actual.Header);
        Assert.Equal(1, actual.TtlSeconds);

        globalOptions.RouteKeys = ["r1"];
        actual = (CacheOptions)method.Invoke(_creator, [route, options, globalOptions, loadBalancingKey]);
        Assert.Equal("route", actual.Region);
        Assert.Equal("route", actual.Header);
        Assert.Equal(1, actual.TtlSeconds);

        globalOptions = null;
        actual = (CacheOptions)method.Invoke(_creator, [route, options, globalOptions, loadBalancingKey]);
        Assert.Equal("route", actual.Region);
        Assert.Equal("route", actual.Header);
        Assert.Equal(1, actual.TtlSeconds);

        // Arrange 3
        options.Header = null;
        globalOptions = new()
        {
            RouteKeys = null,
            Region = "global",
            Header = "global",
            TtlSeconds = 5,
        };
        actual = (CacheOptions)method.Invoke(_creator, [route, options, globalOptions, loadBalancingKey]);
        Assert.Equal("route", actual.Region);
        Assert.Equal("global", actual.Header);
        Assert.Equal(1, actual.TtlSeconds);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2330")] // https://github.com/ThreeMammals/Ocelot/issues/2330
    public void CreateProtected_NoOptions()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        FileDynamicRoute route = new();
        FileCacheOptions options = null;
        FileGlobalCacheOptions globalOptions = null;
        string loadBalancingKey = "lbKey";

        // Act
        var actual = (CacheOptions)method.Invoke(_creator, [route, options, globalOptions, loadBalancingKey]);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(0, actual.TtlSeconds);
        Assert.Null(actual.Region);
        Assert.Null(actual.Header);
        Assert.False(actual.EnableContentHashing);
        Assert.False(actual.UseCache);
    }

    private static FileGlobalConfiguration GivenGlobalConfiguration() => new()
    {
        CacheOptions = new()
        {
            TtlSeconds = 20,
            Region = "globalRegion",
            Header = "globalHeader",
            EnableContentHashing = false,
        },
    };

    private static FileRoute GivenRoute(FileCacheOptions options) => new()
    {
        FileCacheOptions = options,
    };

    private static FileCacheOptions GivenCacheOptions() => new()
    {
        TtlSeconds = 10,
        Region = "region",
        Header = "header",
        EnableContentHashing = true,
    };
}
