using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using System.Reflection;

namespace Ocelot.UnitTests.Configuration;

public class HttpHandlerOptionsCreatorTests : UnitTest
{
    private HttpHandlerOptionsCreator _creator;
    private readonly Mock<IOcelotTracer> _tracer = new();

    public HttpHandlerOptionsCreatorTests()
    {
        Arrange();
    }

    private void Arrange(bool hasTracer = true)
    {
        var services = new ServiceCollection();
        if (hasTracer)
            services.AddSingleton<IOcelotTracer>(_tracer.Object);

        var provider = services.BuildServiceProvider(true);
        _creator = new HttpHandlerOptionsCreator(provider);
    }

    [Fact]
    public void Ctor()
    {
        // Act
        Arrange();

        // Assert
        var field = _creator.GetType().GetField(nameof(_tracer), BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var tracer = field.GetValue(_creator) as IOcelotTracer;
        Assert.NotNull(tracer);
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    public void Create_FileHttpHandlerOptions(bool isNull, bool hasTracer, bool expectedUseTracing)
    {
        Arrange(hasTracer);
        FileHttpHandlerOptions opts = isNull ? null : new()
        {
            UseTracing = true,
        };

        // Act
        var actual = _creator.Create(opts);

        // Assert
        Assert.Equal(expectedUseTracing, actual.UseTracing);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2320")]
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
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
    [Trait("Feat", "585")]
    [Trait("Feat", "2320")]
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    public void Create_FromRoute()
    {
        // Arrange
        var opts = RouteOptions();
        opts.AllowAutoRedirect = null;
        opts.MaxConnectionsPerServer = null;
        var route = GivenRoute(opts);

        // Act
        var actual = _creator.Create(route, GlobalConfiguration());

        // Assert
        Assert.False(actual.AllowAutoRedirect);
        Assert.Equal(111, actual.MaxConnectionsPerServer);
        Assert.Equal(333, actual.PooledConnectionLifeTime.TotalSeconds);
        Assert.True(actual.UseCookieContainer);
        Assert.True(actual.UseProxy);
        Assert.True(actual.UseTracing);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2320")]
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
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
    [Trait("Feat", "585")]
    [Trait("Feat", "2320")]
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    public void Create_FromDynamicRoute()
    {
        // Arrange
        var opts = RouteOptions();
        opts.AllowAutoRedirect = null;
        opts.MaxConnectionsPerServer = null;
        var route = GivenDynamicRoute(opts);

        // Act
        var actual = _creator.Create(route, GlobalConfiguration());

        // Assert
        Assert.False(actual.AllowAutoRedirect);
        Assert.Equal(111, actual.MaxConnectionsPerServer);
        Assert.Equal(333, actual.PooledConnectionLifeTime.TotalSeconds);
        Assert.True(actual.UseCookieContainer);
        Assert.True(actual.UseProxy);
        Assert.True(actual.UseTracing);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2320")]
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    public void CreateProtected_NullCheck()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        IRouteGrouping grouping = null;
        FileHttpHandlerOptions options = null;
        FileGlobalHttpHandlerOptions globalOptions = null;

        // Act
        var wrapper = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(_creator, [grouping, options, globalOptions]));

        // Assert
        Assert.IsType<ArgumentNullException>(wrapper.InnerException);
        var actual = (ArgumentNullException)wrapper.InnerException;
        Assert.Equal(nameof(grouping), actual.ParamName);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2320")]
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    public void CreateProtected()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        FileDynamicRoute route = new() { Key = "r1" };
        FileHttpHandlerOptions options = null;
        var configuration = GlobalConfiguration();
        FileGlobalHttpHandlerOptions globalOptions = configuration.HttpHandlerOptions;

        // Act, Assert
        var actual = (HttpHandlerOptions)method.Invoke(_creator, [route, options, globalOptions]);
        Assert.False(actual.AllowAutoRedirect); // global
        Assert.Equal(111, actual.MaxConnectionsPerServer);
        Assert.Equal(111, actual.PooledConnectionLifeTime.TotalSeconds);
        Assert.False(actual.UseCookieContainer);
        Assert.False(actual.UseProxy);
        Assert.False(actual.UseTracing);

        // Arrange 2
        options = RouteOptions();
        globalOptions.RouteKeys = ["?"];

        // Act, Assert 2
        actual = (HttpHandlerOptions)method.Invoke(_creator, [route, options, globalOptions]);
        Assert.True(actual.AllowAutoRedirect); // route
        Assert.Equal(333, actual.MaxConnectionsPerServer);
        Assert.Equal(333, actual.PooledConnectionLifeTime.TotalSeconds);
        Assert.True(actual.UseCookieContainer);
        Assert.True(actual.UseProxy);
        Assert.True(actual.UseTracing);

        globalOptions.RouteKeys = ["r1"];
        actual = (HttpHandlerOptions)method.Invoke(_creator, [route, options, globalOptions]);
        Assert.True(actual.AllowAutoRedirect); // route
        Assert.Equal(333, actual.MaxConnectionsPerServer);
        Assert.Equal(333, actual.PooledConnectionLifeTime.TotalSeconds);
        Assert.True(actual.UseCookieContainer);
        Assert.True(actual.UseProxy);
        Assert.True(actual.UseTracing);

        globalOptions = null;
        actual = (HttpHandlerOptions)method.Invoke(_creator, [route, options, globalOptions]);
        Assert.True(actual.AllowAutoRedirect); // route
        Assert.Equal(333, actual.MaxConnectionsPerServer);
        Assert.Equal(333, actual.PooledConnectionLifeTime.TotalSeconds);
        Assert.True(actual.UseCookieContainer);
        Assert.True(actual.UseProxy);
        Assert.True(actual.UseTracing);

        // Arrange 3
        options.MaxConnectionsPerServer = null; // -> global
        globalOptions = configuration.HttpHandlerOptions;
        globalOptions.RouteKeys = null;
        actual = (HttpHandlerOptions)method.Invoke(_creator, [route, options, globalOptions]);
        Assert.Equal(111, actual.MaxConnectionsPerServer); // global
        Assert.True(actual.AllowAutoRedirect); // route
        Assert.Equal(333, actual.PooledConnectionLifeTime.TotalSeconds);
        Assert.True(actual.UseCookieContainer);
        Assert.True(actual.UseProxy);
        Assert.True(actual.UseTracing);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2320")]
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    public void CreateProtected_NoOptions()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        FileDynamicRoute route = new();
        FileHttpHandlerOptions options = null;
        FileGlobalHttpHandlerOptions globalOptions = null;

        // Act
        var actual = (HttpHandlerOptions)method.Invoke(_creator, [route, options, globalOptions]);

        // Assert : parameterless constructor was called
        Assert.Equal(int.MaxValue, actual.MaxConnectionsPerServer);
        Assert.Equal(HttpHandlerOptions.DefaultPooledConnectionLifetimeSeconds, actual.PooledConnectionLifeTime.TotalSeconds);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2320")]
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332

    public void Merge_NullCheck()
    {
        // Arrange
        var method = _creator.GetType().GetMethod(nameof(Merge), BindingFlags.Instance | BindingFlags.NonPublic);
        FileHttpHandlerOptions options = null;
        FileHttpHandlerOptions globalOptions = null;

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
    [Trait("Feat", "585")]
    [Trait("Feat", "2320")]
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public void Merge(bool isDef, bool hasTracer)
    {
        // Arrange
        Arrange(hasTracer);
        var method = _creator.GetType().GetMethod(nameof(Merge), BindingFlags.Instance | BindingFlags.NonPublic);
        FileHttpHandlerOptions options = new()
        {
            AllowAutoRedirect = isDef ? null : true,
            MaxConnectionsPerServer = isDef ? null : 333,
            PooledConnectionLifetimeSeconds = isDef ? null : 333,
            UseCookieContainer = isDef ? null : true,
            UseProxy = isDef ? null : true,
            UseTracing = isDef ? null : true,
        };
        FileHttpHandlerOptions globalOptions = new()
        {
            AllowAutoRedirect = isDef ? null : false,
            MaxConnectionsPerServer = isDef ? null : 111,
            PooledConnectionLifetimeSeconds = isDef ? null : 111,
            UseCookieContainer = isDef ? null : false,
            UseProxy = isDef ? null : false,
            UseTracing = isDef ? null : false,
        };

        // Act
        var actual = (HttpHandlerOptions)method.Invoke(_creator, [options, globalOptions]);

        // Assert
        Assert.Equal(!isDef, actual.AllowAutoRedirect);
        Assert.Equal(isDef ? int.MaxValue : 333, actual.MaxConnectionsPerServer);
        Assert.Equal(isDef ? HttpHandlerOptions.DefaultPooledConnectionLifetimeSeconds : 333, actual.PooledConnectionLifeTime.TotalSeconds);
        Assert.Equal(!isDef, actual.UseCookieContainer);
        Assert.Equal(!isDef, actual.UseProxy);
        Assert.Equal(hasTracer && !isDef, actual.UseTracing); // the useTracing parameter takes absolute priority
    }

    private static FileHttpHandlerOptions RouteOptions() => new()
    {
        AllowAutoRedirect = true,
        MaxConnectionsPerServer = 333,
        PooledConnectionLifetimeSeconds = 333,
        UseCookieContainer = true,
        UseProxy = true,
        UseTracing = true,
    };

    private static FileGlobalConfiguration GlobalConfiguration() => new()
    {
        HttpHandlerOptions = new()
        {
            RouteKeys = null,
            AllowAutoRedirect = false,
            MaxConnectionsPerServer = 111,
            PooledConnectionLifetimeSeconds = 111,
            UseCookieContainer = false,
            UseProxy = false,
            UseTracing = false,
        },
    };

    private static FileRoute GivenRoute(FileHttpHandlerOptions options = null)
        => new() { HttpHandlerOptions = options ?? new() };
    private static FileDynamicRoute GivenDynamicRoute(FileHttpHandlerOptions options = null)
        => new() { HttpHandlerOptions = options ?? new() };
}
