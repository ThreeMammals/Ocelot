using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class AuthenticationOptionsCreatorTests
{
    private readonly AuthenticationOptionsCreator _authOptionsCreator;

    private readonly List<string> _routeScopes = new() { "route scope 1", "route scope 2" };
    private const string _routeAuthProviderKey = "route key";
    private readonly string[] _routeAuthProviderKeys = new string[] { "route key 1", "route key 2" };

    private readonly List<string> _globalScopes = new() { "global scope 1", "global scope 2" };
    private const string _globalAuthProviderKey = "global key";
    private readonly string[] _globalAuthProviderKeys = new string[] { "global key 1", "global key 2" };

    public AuthenticationOptionsCreatorTests()
    {
        _authOptionsCreator = new AuthenticationOptionsCreator();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_OptionsObjIsNull_CreatedSuccessfullyWithEmptyCollections(bool createRoute)
    {
        // Arrange
        FileRoute route = createRoute ? new() : null;
        FileAuthenticationOptions options = null;
        if (createRoute && route != null)
        {
            route.AuthenticationOptions = options;
        }

        // Act
        var actual = _authOptionsCreator.Create(route, null);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.AllowedScopes);
        Assert.Empty(actual.AllowedScopes);
        Assert.NotNull(actual.AuthenticationProviderKeys);
        Assert.Empty(actual.AuthenticationProviderKeys);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_OptionsObjIsNotNull_CreatedSuccessfully(bool isAuthenticationProviderKeys)
    {
        string authenticationProviderKey = !isAuthenticationProviderKeys ? _routeAuthProviderKey : null;
        string[] authenticationProviderKeys = isAuthenticationProviderKeys ? _routeAuthProviderKeys : null;
        var route = CreateFileRoute(_routeScopes, authenticationProviderKey, authenticationProviderKeys);
        var expected = new AuthenticationOptions(route.AuthenticationOptions);

        // Act
        var actual = _authOptionsCreator.Create(route, null);

        // Assert
        actual.AllowedScopes.ShouldBe(expected.AllowedScopes);
        actual.AuthenticationProviderKeys.ShouldBe(expected.AuthenticationProviderKeys);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [Trait("PR", "2114")]
    [Trait("Feat", "842")]
    public void Create_ArgumentIsNull_CreatedSuccessfully(bool is1st, bool is2nd)
    {
        // Arrange
        FileRoute arg1 = is1st ? new() : null;
        FileGlobalConfiguration arg2 = is2nd ? new() : null;

        // Act
        var actual = _authOptionsCreator.Create(arg1, arg2);

        // Assert
        Assert.NotNull(actual);
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
        var actual = _authOptionsCreator.Create(arg1, arg2);

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
        var actual = _authOptionsCreator.Create(route, globalConfig);

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
        var actual = _authOptionsCreator.Create(route, globalConfig);

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
        var actual = _authOptionsCreator.Create(route, globalConfig);

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
        var actual = _authOptionsCreator.Create(null, null);
        Assert.NotNull(actual);

        FileRoute route = new();
        FileGlobalConfiguration global = new();
        actual = _authOptionsCreator.Create(route, global);
        Assert.NotNull(actual);

        route.AuthenticationOptions = global.AuthenticationOptions = null;
        actual = _authOptionsCreator.Create(route, global);
        Assert.NotNull(actual);

        route.AuthenticationOptions = new();
        global.AuthenticationOptions = new()
        {
            AllowedScopes = ["test"],
            AuthenticationProviderKeys = ["test"],
        };
        actual = _authOptionsCreator.Create(route, global);
        Assert.NotNull(actual);

        route.AuthenticationOptions.AuthenticationProviderKeys = ["test"];
        route.AuthenticationOptions.AllowedScopes = ["test"];
        global.AuthenticationOptions.AuthenticationProviderKeys = [];
        global.AuthenticationOptions.AllowedScopes = [];
        actual = _authOptionsCreator.Create(route, global);
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
