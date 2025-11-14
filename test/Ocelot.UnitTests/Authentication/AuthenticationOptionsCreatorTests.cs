using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

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

    [Fact]
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

    [Fact]
    [Trait("Feat", "2316")] // https://github.com/ThreeMammals/Ocelot/issues/2316
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("PR", "2114")]
    [Trait("Feat", "842")]
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
