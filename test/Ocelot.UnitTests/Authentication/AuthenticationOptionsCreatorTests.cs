using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using System.Reflection;

namespace Ocelot.UnitTests.Authentication;

public class AuthenticationOptionsCreatorTests
{
    private readonly AuthenticationOptionsCreator _creator;

    private readonly List<string> _routeScopes = new() { "route scope 1", "route scope 2" };
    private const string _routeAuthProviderKey = "route key";
    private readonly string[] _routeAuthProviderKeys = new string[] { "route key 1", "route key 2" };

    private readonly List<string> _globalScopes = new() { "global scope 1", "global scope 2" };
    private const string _globalAuthProviderKey = "global key";
    private readonly string[] _globalAuthProviderKeys = new string[] { "global key 1", "global key 2" };

    public AuthenticationOptionsCreatorTests()
    {
        _creator = new AuthenticationOptionsCreator();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_OptionsObjIsNotNull_CreatedFromRoute(bool isAuthenticationProviderKeys)
    {
        string authenticationProviderKey = !isAuthenticationProviderKeys ? _routeAuthProviderKey : null;
        string[] authenticationProviderKeys = isAuthenticationProviderKeys ? _routeAuthProviderKeys : null;
        var route = CreateFileRoute(_routeScopes, authenticationProviderKey, authenticationProviderKeys);

        // Act
        var actual = _creator.Create(route, new());

        // Assert
        actual.AllowedScopes.ShouldBe(_routeScopes);
        var expectedKeys = Enumerable
            .Repeat(authenticationProviderKey, !isAuthenticationProviderKeys ? 1 : 0)
            .Concat(authenticationProviderKeys ?? Enumerable.Empty<string>())
            .ToArray();
        actual.AuthenticationProviderKeys.ShouldBe(expectedKeys);
    }

    #region PR 2114
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("PR", "2114")] // https://github.com/ThreeMammals/Ocelot/pull/2114
    [Trait("Feat", "842")] // https://github.com/ThreeMammals/Ocelot/issues/842
    public void Create_GlobalAuthOptsObjIsNull_CreatedSuccessfully(bool isNull)
    {
        // Arrange
        FileRoute arg1 = new();
        FileGlobalConfiguration arg2 = new()
        {
            AuthenticationOptions = isNull ? new() : null,
        };

        // Act
        var actual = _creator.Create(arg1, arg2);

        // Assert
        Assert.NotNull(actual);
    }

    [Fact]
    [Trait("PR", "2114")]
    [Trait("Feat", "842")]
    public void Create_GlobalConfigExists_ShouldUseGlobal()
    {
        // Arrange
        var route = new FileRoute();
        var globalConfig = CreateGlobalConfiguration(_globalScopes, _globalAuthProviderKey, _globalAuthProviderKeys);
        var expected = new AuthenticationOptions(globalConfig.AuthenticationOptions);

        // Act
        var actual = _creator.Create(route, globalConfig);

        // Assert
        actual.AllowedScopes.ShouldBe(expected.AllowedScopes);
        actual.AuthenticationProviderKeys.ShouldBe(expected.AuthenticationProviderKeys);
    }

    [Fact]
    [Trait("PR", "2114")]
    [Trait("Feat", "842")]
    public void Create_RouteKeyProviderEmpty_ShouldUseGlobal()
    {
        // Arrange
        var route = CreateFileRoute(_routeScopes, string.Empty, null);
        var globalConfig = CreateGlobalConfiguration(_globalScopes, _globalAuthProviderKey, _globalAuthProviderKeys);

        // Act
        var actual = _creator.Create(route, globalConfig);

        // Assert
        actual.AllowedScopes.ShouldBe(_routeScopes);
        actual.AuthenticationProviderKeys.ShouldContain(_globalAuthProviderKey);
        actual.AuthenticationProviderKeys.ShouldContain(_globalAuthProviderKeys[0]);
        actual.AuthenticationProviderKeys.ShouldContain(_globalAuthProviderKeys[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("PR", "2114")]
    [Trait("Feat", "842")]
    public void Create_RouteAndGlobalKeyExist_ShouldUseRoute(bool routeHasSingleProviderKey)
    {
        // Arrange
        var routeAuthProviderKey = routeHasSingleProviderKey ? _routeAuthProviderKey : null;
        var routeAuthProviderKeys = routeHasSingleProviderKey ? null : _routeAuthProviderKeys;
        var route = CreateFileRoute(_routeScopes, routeAuthProviderKey, routeAuthProviderKeys);
        var globalConfig = CreateGlobalConfiguration(_globalScopes, _globalAuthProviderKey, _globalAuthProviderKeys);

        // Act
        var actual = _creator.Create(route, globalConfig);

        // Assert
        actual.AllowedScopes.ShouldBe(_routeScopes);
        if (routeHasSingleProviderKey)
        {
            actual.AuthenticationProviderKeys.ShouldContain(_routeAuthProviderKey);
            actual.AuthenticationProviderKeys.ShouldNotContain(_routeAuthProviderKeys[0]);
            actual.AuthenticationProviderKeys.ShouldNotContain(_routeAuthProviderKeys[1]);
        }
        else
        {
            actual.AuthenticationProviderKeys.ShouldNotContain(_routeAuthProviderKey);
            actual.AuthenticationProviderKeys.ShouldContain(_routeAuthProviderKeys[0]);
            actual.AuthenticationProviderKeys.ShouldContain(_routeAuthProviderKeys[1]);
        }
    }

    [Fact]
    [Trait("PR", "2114")]
    [Trait("Feat", "842")]
    public void Create()
    {
        // Arrange
        FileRoute route = null;
        FileGlobalConfiguration globalConfiguration = null;
        var ex = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(route), ex.ParamName);
        route = new();
        ex = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(globalConfiguration), ex.ParamName);

        globalConfiguration = new();
        var actual = _creator.Create(route, globalConfiguration);
        Assert.NotNull(actual);

        route.AuthenticationOptions = globalConfiguration.AuthenticationOptions = null;
        actual = _creator.Create(route, globalConfiguration);
        Assert.NotNull(actual);

        route.AuthenticationOptions = new();
        globalConfiguration.AuthenticationOptions = new()
        {
            AllowedScopes = ["test"],
            AuthenticationProviderKeys = ["test"],
        };
        actual = _creator.Create(route, globalConfiguration);
        Assert.NotNull(actual);

        route.AuthenticationOptions.AuthenticationProviderKeys = ["test"];
        route.AuthenticationOptions.AllowedScopes = ["test"];
        globalConfiguration.AuthenticationOptions.AuthenticationProviderKeys = [];
        globalConfiguration.AuthenticationOptions.AllowedScopes = [];
        actual = _creator.Create(route, globalConfiguration);
        Assert.NotNull(actual);
    }
    #endregion PR 2114

    #region PR 2336
    [Fact]
    [Trait("PR", "2336")] // https://github.com/ThreeMammals/Ocelot/pull/2336
    [Trait("Feat", "2316")] // https://github.com/ThreeMammals/Ocelot/issues/2316
    public void Create_FileAuthenticationOptions()
    {
        // Arrange
        FileAuthenticationOptions options = new()
        {
            AllowAnonymous = true,
            AllowedScopes = new() { "scope" },
            AuthenticationProviderKey = "key1",
            AuthenticationProviderKeys = new[] { "key2" },
        };

        // Act
        var actual = _creator.Create(options);

        // Assert
        Assert.NotNull(actual);
        Assert.True(actual.AllowAnonymous);
        Assert.Contains("scope", actual.AllowedScopes);
        Assert.Contains("key1", actual.AuthenticationProviderKeys);
        Assert.Contains("key2", actual.AuthenticationProviderKeys);
    }

    [Fact]
    [Trait("PR", "2336")]
    [Trait("Feat", "2316")]
    public void Create_FileRoute_ArgumentNullChecks()
    {
        // Arrange
        FileRoute route = null;
        FileGlobalConfiguration globalConfiguration = null;

        // Act, Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(route), ex.ParamName);

        // Act, Assert
        route = new();
        ex = Assert.Throws<ArgumentNullException>(() => _creator.Create(route, globalConfiguration));
        Assert.Equal(nameof(globalConfiguration), ex.ParamName);
    }

    [Fact]
    [Trait("PR", "2336")]
    [Trait("Feat", "2316")]
    public void Create_FromRoute()
    {
        // Arrange
        FileRoute route = new()
        {
            AuthenticationOptions = new()
            {
                AllowAnonymous = false,
                AllowedScopes = null,
                AuthenticationProviderKey = "route",
                AuthenticationProviderKeys = null,
            },
        };
        FileGlobalConfiguration globalConfiguration = new()
        {
            AuthenticationOptions = new()
            {
                AllowAnonymous = null,
                AllowedScopes = ["global"],
                AuthenticationProviderKey = null,
                AuthenticationProviderKeys = ["global"],
            },
        };

        // Act
        var actual = _creator.Create(route, globalConfiguration);

        // Assert
        Assert.False(actual.AllowAnonymous);
        Assert.Contains("global", actual.AllowedScopes);
        Assert.Contains("route", actual.AuthenticationProviderKeys);
        Assert.Contains("global", actual.AuthenticationProviderKeys);
    }

    [Fact]
    [Trait("PR", "2336")]
    [Trait("Feat", "2316")]
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
    [Trait("PR", "2336")]
    [Trait("Feat", "2316")]
    public void Create_FromDynamicRoute()
    {
        // Arrange
        FileDynamicRoute route = new()
        {
            AuthenticationOptions = new()
            {
                AllowAnonymous = false,
                AllowedScopes = null,
                AuthenticationProviderKey = "route",
                AuthenticationProviderKeys = null,
            },
        };
        FileGlobalConfiguration globalConfiguration = new()
        {
            AuthenticationOptions = new()
            {
                AllowAnonymous = null,
                AllowedScopes = ["global"],
                AuthenticationProviderKey = null,
                AuthenticationProviderKeys = ["global"],
            },
        };

        // Act
        var actual = _creator.Create(route, globalConfiguration);

        // Assert
        Assert.False(actual.AllowAnonymous);
        Assert.Contains("global", actual.AllowedScopes);
        Assert.Contains("route", actual.AuthenticationProviderKeys);
        Assert.Contains("global", actual.AuthenticationProviderKeys);
    }

    [Fact]
    [Trait("PR", "2336")]
    [Trait("Feat", "2316")]
    public void CreateProtected_NullCheck()
    {
        // Arrange
        var method = _creator.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic);
        IRouteGrouping grouping = null;
        FileAuthenticationOptions options = null;
        FileGlobalAuthenticationOptions globalOptions = null;

        // Act
        var wrapper = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(_creator, [grouping, options, globalOptions]));

        // Assert
        Assert.IsType<ArgumentNullException>(wrapper.InnerException);
        var actual = (ArgumentNullException)wrapper.InnerException;
        Assert.Equal(nameof(grouping), actual.ParamName);
    }

    [Fact]
    [Trait("PR", "2336")]
    [Trait("Feat", "2316")]
    public void CreateProtected()
    {
        // Arrange
        FileAuthenticationOptions options = null;
        FileDynamicRoute route = new()
        {
            Key = "r1",
            AuthenticationOptions = options,
        };
        FileGlobalAuthenticationOptions globalOptions = new()
        {
            RouteKeys = null,
            AllowAnonymous = null,
            AllowedScopes = ["global"],
            AuthenticationProviderKey = "globalKey",
            AuthenticationProviderKeys = ["global1", "global2"],
        };
        FileGlobalConfiguration globalConfiguration = new()
        {
            AuthenticationOptions = globalOptions,
        };

        // Act, Assert
        var actual = _creator.Create(route, globalConfiguration);
        Assert.False(actual.AllowAnonymous);
        Assert.Contains("global", actual.AllowedScopes);
        Assert.Contains("globalKey", actual.AuthenticationProviderKeys);
        Assert.Contains("global1", actual.AuthenticationProviderKeys);
        Assert.Contains("global2", actual.AuthenticationProviderKeys);

        // Arrange 2
        route.AuthenticationOptions = options = new()
        {
            AllowAnonymous = true,
            AllowedScopes = ["route"],
            AuthenticationProviderKey = "route",
            AuthenticationProviderKeys = ["route1", "route2"],
        };
        globalOptions.RouteKeys = ["?"];

        // Act, Assert 2
        actual = _creator.Create(route, globalConfiguration);
        Assert.True(actual.AllowAnonymous);
        Assert.Contains("route", actual.AllowedScopes);
        Assert.Contains("route", actual.AuthenticationProviderKeys);
        Assert.Contains("route1", actual.AuthenticationProviderKeys);
        Assert.Contains("route2", actual.AuthenticationProviderKeys);

        globalOptions.RouteKeys = ["r1"];
        actual = _creator.Create(route, globalConfiguration);
        Assert.True(actual.AllowAnonymous);
        Assert.Contains("route", actual.AllowedScopes);
        Assert.Contains("route", actual.AuthenticationProviderKeys);
        Assert.Contains("route1", actual.AuthenticationProviderKeys);
        Assert.Contains("route2", actual.AuthenticationProviderKeys);

        globalConfiguration.AuthenticationOptions = globalOptions = null;
        actual = _creator.Create(route, globalConfiguration);
        Assert.True(actual.AllowAnonymous);
        Assert.Contains("route", actual.AllowedScopes);
        Assert.Contains("route", actual.AuthenticationProviderKeys);
        Assert.Contains("route1", actual.AuthenticationProviderKeys);
        Assert.Contains("route2", actual.AuthenticationProviderKeys);

        // Arrange 3 : Merging
        options.AllowAnonymous = null;
        options.AllowedScopes = null;
        options.AuthenticationProviderKey = null;
        globalConfiguration.AuthenticationOptions = globalOptions = new()
        {
            RouteKeys = null,
            AllowAnonymous = false,
            AllowedScopes = ["global"],
            AuthenticationProviderKey = "globalKey",
            AuthenticationProviderKeys = ["global1", "global2"],
        };
        actual = _creator.Create(route, globalConfiguration);
        Assert.False(actual.AllowAnonymous);
        Assert.Contains("global", actual.AllowedScopes);
        Assert.Contains("globalKey", actual.AuthenticationProviderKeys);
        Assert.Contains("route1", actual.AuthenticationProviderKeys);
        Assert.Contains("route2", actual.AuthenticationProviderKeys);
    }
    #endregion PR 2336

    private static FileRoute CreateFileRoute(List<string> allowedScopes, string authProviderKey, string[] authProviderKeys) => new()
    {
        AuthenticationOptions = new()
        {
            AllowedScopes = allowedScopes,
            AuthenticationProviderKey = authProviderKey,
            AuthenticationProviderKeys = authProviderKeys,
        },
    };

    private static FileGlobalConfiguration CreateGlobalConfiguration(List<string> allowedScopes, string authProviderKey, string[] authProviderKeys) => new()
    {
        AuthenticationOptions = new()
        {
            AllowedScopes = allowedScopes,
            AuthenticationProviderKey = authProviderKey,
            AuthenticationProviderKeys = authProviderKeys,
        },
    };
}
