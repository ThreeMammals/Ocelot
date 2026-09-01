using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using System.Reflection;

namespace Ocelot.UnitTests.Configuration;

public class WebSocketOptionsCreatorTests : UnitTest
{
    private readonly WebSocketOptionsCreator _creator = new();

    [Theory]
    [InlineData(null, null)]
    [InlineData(65536, 65536)]
    public void Create_FileWebSocketOptions(int? bufferSize, int? expected)
    {
        // Arrange
        FileWebSocketOptions opts = bufferSize is null ? null : new() { BufferSize = bufferSize };

        // Act
        var actual = _creator.Create(opts);

        // Assert
        Assert.Equal(expected, actual.BufferSize);
    }

    [Fact]
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
    public void Create_FromRoute_RouteValueOverridesGlobal()
    {
        // Arrange
        var route = GivenRoute(RouteOptions());

        // Act
        var actual = _creator.Create(route, GlobalConfiguration());

        // Assert
        Assert.Equal(65536, actual.BufferSize); // route wins over global's 4096
    }

    [Fact]
    public void Create_FromRoute_NullRouteValueInheritsGlobal()
    {
        // Arrange
        var route = GivenRoute(options: null);

        // Act
        var actual = _creator.Create(route, GlobalConfiguration());

        // Assert
        Assert.Equal(4096, actual.BufferSize); // inherited from global
    }

    [Fact]
    public void Create_FromRoute_RouteValueWithoutGlobalSection()
    {
        // Arrange: route has options but no global WebSocket section is defined
        var route = GivenRoute(RouteOptions());
        var configuration = new FileGlobalConfiguration { WebSocket = null };

        // Act
        var actual = _creator.Create(route, configuration);

        // Assert
        Assert.Equal(65536, actual.BufferSize); // route value used as-is, nothing to merge
    }

    [Fact]
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
    public void Create_FromDynamicRoute_RouteValueOverridesGlobal()
    {
        // Arrange
        var route = GivenDynamicRoute(RouteOptions());

        // Act
        var actual = _creator.Create(route, GlobalConfiguration());

        // Assert
        Assert.Equal(65536, actual.BufferSize); // route wins over global's 4096
    }

    [Fact]
    public void Create_FromDynamicRoute_NullRouteValueInheritsGlobal()
    {
        // Arrange
        var route = GivenDynamicRoute(options: null);

        // Act
        var actual = _creator.Create(route, GlobalConfiguration());

        // Assert
        Assert.Equal(4096, actual.BufferSize); // inherited from global
    }

    [Fact]
    public void CreateProtected_NullCheck()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        IRouteGrouping grouping = null;
        FileWebSocketOptions options = null;
        FileGlobalWebSocketOptions globalOptions = null;

        // Act
        var wrapper = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(_creator, [grouping, options, globalOptions]));

        // Assert
        Assert.IsType<ArgumentNullException>(wrapper.InnerException);
        var actual = (ArgumentNullException)wrapper.InnerException;
        Assert.Equal(nameof(grouping), actual.ParamName);
    }

    [Fact]
    public void CreateProtected_RouteKeys_AppliesOnlyToMatchingRoutes()
    {
        // Arrange: Create/Merge mutate the FileWebSocketOptions instance they're given
        // (same as QoSOptionsCreator/HttpHandlerOptionsCreator), so each case below uses a fresh instance.
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        FileDynamicRoute route = new() { Key = "r1" };
        var configuration = GlobalConfiguration();
        FileGlobalWebSocketOptions globalOptions = configuration.WebSocket;

        // Act: route not in the group -> route's own value wins regardless
        globalOptions.RouteKeys = ["r2"]; // group targets a different route
        var actual = (WebSocketOptions)method.Invoke(_creator, [route, RouteOptions(), globalOptions]);
        Assert.Equal(65536, actual.BufferSize);

        // Act 2: route IS in the group -> merge applies (route still wins since it's set)
        globalOptions.RouteKeys = ["r1"];
        actual = (WebSocketOptions)method.Invoke(_creator, [route, RouteOptions(), globalOptions]);
        Assert.Equal(65536, actual.BufferSize);

        // Act 3: route in the group, route value unset -> falls back to global
        actual = (WebSocketOptions)method.Invoke(_creator, [route, new FileWebSocketOptions { BufferSize = null }, globalOptions]);
        Assert.Equal(4096, actual.BufferSize);

        // Act 4: route NOT in the group, route value unset -> global does not apply, default null
        globalOptions.RouteKeys = ["r2"];
        actual = (WebSocketOptions)method.Invoke(_creator, [route, new FileWebSocketOptions { BufferSize = null }, globalOptions]);
        Assert.Null(actual.BufferSize);
    }

    [Fact]
    public void CreateProtected_NoOptions()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        FileDynamicRoute route = new();
        FileWebSocketOptions options = null;
        FileGlobalWebSocketOptions globalOptions = null;

        // Act
        var actual = (WebSocketOptions)method.Invoke(_creator, [route, options, globalOptions]);

        // Assert: parameterless constructor was called
        Assert.Null(actual.BufferSize);
    }

    [Fact]
    public void Merge_NullCheck()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Merge", BindingFlags.Instance | BindingFlags.NonPublic);

        // Act, Assert: null options/global are coalesced to new(), not thrown
        var actual = (WebSocketOptions)method.Invoke(_creator, [null, null]);
        Assert.Null(actual.BufferSize);
    }

    [Fact]
    public void Merge_RouteValueTakesPrecedence()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Merge", BindingFlags.Instance | BindingFlags.NonPublic);
        FileWebSocketOptions options = new() { BufferSize = 65536 };
        FileWebSocketOptions globalOptions = new() { BufferSize = 4096 };

        // Act
        var actual = (WebSocketOptions)method.Invoke(_creator, [options, globalOptions]);

        // Assert
        Assert.Equal(65536, actual.BufferSize);
    }

    [Fact]
    public void Merge_NullRouteValueFallsBackToGlobal()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Merge", BindingFlags.Instance | BindingFlags.NonPublic);
        FileWebSocketOptions options = new() { BufferSize = null };
        FileWebSocketOptions globalOptions = new() { BufferSize = 4096 };

        // Act
        var actual = (WebSocketOptions)method.Invoke(_creator, [options, globalOptions]);

        // Assert
        Assert.Equal(4096, actual.BufferSize);
    }

    private static FileWebSocketOptions RouteOptions() => new()
    {
        BufferSize = 65536,
    };

    private static FileGlobalConfiguration GlobalConfiguration() => new()
    {
        WebSocket = new()
        {
            RouteKeys = null,
            BufferSize = 4096,
        },
    };

    private static FileRoute GivenRoute(FileWebSocketOptions options = null)
        => new() { WebSocket = options };
    private static FileDynamicRoute GivenDynamicRoute(FileWebSocketOptions options = null)
        => new() { WebSocket = options };
}
