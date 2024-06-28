using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using System.Security.Permissions;

namespace Ocelot.UnitTests.Configuration;

public class AuthenticationOptionsCreatorTests
{
    private readonly AuthenticationOptionsCreator _authOptionsCreator;

    private readonly List<string> _routeScopes = new List<string> { "route scope 1", "route scope 2" };
    private const string _routeAuthProviderKey = "route key";
    private readonly string[] _routeAuthProviderKeys = new string[] { "route key 1", "route key 2" };

    private readonly List<string> _globalScopes = new List<string> { "global scope 1", "global scope 2" };
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
        var actual = _authOptionsCreator.Create(route?.AuthenticationOptions, null);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.AllowedScopes);
        Assert.Empty(actual.AllowedScopes);
        Assert.NotNull(actual.AuthenticationProviderKey);
        Assert.NotNull(actual.AuthenticationProviderKeys);
        Assert.Empty(actual.AuthenticationProviderKeys);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_OptionsObjIsNotNull_CreatedSuccessfully(bool isAuthenticationProviderKeys)
    {
        // Arrange
        string authenticationProviderKey = !isAuthenticationProviderKeys ? _routeAuthProviderKey : null;
        string[] authenticationProviderKeys = isAuthenticationProviderKeys ? _routeAuthProviderKeys : null;
        var fileRoute = AuthenticationOptionsCreatorTests.CreateFileRoute(_routeScopes, authenticationProviderKey, authenticationProviderKeys);
        var expected = new AuthenticationOptionsBuilder()
            .WithAllowedScopes(fileRoute.AuthenticationOptions?.AllowedScopes)
            .WithAuthenticationProviderKey(authenticationProviderKey)
            .WithAuthenticationProviderKeys(authenticationProviderKeys)
            .Build();

        // Act
        var actual = _authOptionsCreator.Create(fileRoute.AuthenticationOptions, null);

        // Assert
        actual.AllowedScopes.ShouldBe(expected.AllowedScopes);
        actual.AuthenticationProviderKey.ShouldBe(expected.AuthenticationProviderKey);
        actual.AuthenticationProviderKeys.ShouldBe(expected.AuthenticationProviderKeys);
    }

    [Fact]
    public void Create_GlobalConfigExists_ShouldUseGlobal()
    {
        // Arrange
        var route = new FileRoute();
        var globalConfig = AuthenticationOptionsCreatorTests.CreateGlobalConfiguration(_globalScopes, _globalAuthProviderKey, _globalAuthProviderKeys);
        var expected = new AuthenticationOptionsBuilder()
            .WithAllowedScopes(_globalScopes)
            .WithAuthenticationProviderKey(_globalAuthProviderKey)
            .WithAuthenticationProviderKeys(_globalAuthProviderKeys)
            .Build();

        // Act
        var actual = _authOptionsCreator.Create(route.AuthenticationOptions, globalConfig.AuthenticationOptions);

        // Assert
        actual.AllowedScopes.ShouldBe(expected.AllowedScopes);
        actual.AuthenticationProviderKey.ShouldBe(expected.AuthenticationProviderKey);
        actual.AuthenticationProviderKeys.ShouldBe(expected.AuthenticationProviderKeys);
    }

    [Fact]
    public void Create_RouteKeyProviderEmpty_ShouldUseGlobal()
    {
        // Arrange
        var route = AuthenticationOptionsCreatorTests.CreateFileRoute(_routeScopes, string.Empty, null);
        var globalConfig = AuthenticationOptionsCreatorTests.CreateGlobalConfiguration(_globalScopes, _globalAuthProviderKey, _globalAuthProviderKeys);
        var expected = new AuthenticationOptionsBuilder()
            .WithAllowedScopes(_globalScopes)
            .WithAuthenticationProviderKey(_globalAuthProviderKey)
            .WithAuthenticationProviderKeys(_globalAuthProviderKeys)
            .Build();

        // Act
        var actual = _authOptionsCreator.Create(route.AuthenticationOptions, globalConfig.AuthenticationOptions);

        // Assert
        actual.AllowedScopes.ShouldBe(expected.AllowedScopes);
        actual.AuthenticationProviderKey.ShouldBe(expected.AuthenticationProviderKey);
        actual.AuthenticationProviderKeys.ShouldBe(expected.AuthenticationProviderKeys);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_RouteAndGlobalKeyExist_ShouldUseRoute(bool routeHasSingleProviderKey)
    {
        // Arrange
        var routeAuthProviderKey = routeHasSingleProviderKey ? _routeAuthProviderKey : null;
        var routeAuthProviderKeys = routeHasSingleProviderKey ? null : _routeAuthProviderKeys;
        var route = AuthenticationOptionsCreatorTests.CreateFileRoute(_routeScopes, routeAuthProviderKey, routeAuthProviderKeys);
        var globalConfig = AuthenticationOptionsCreatorTests.CreateGlobalConfiguration(_globalScopes, _globalAuthProviderKey, _globalAuthProviderKeys);
        var expected = new AuthenticationOptionsBuilder()
            .WithAllowedScopes(_routeScopes)
            .WithAuthenticationProviderKey(routeAuthProviderKey)
            .WithAuthenticationProviderKeys(routeAuthProviderKeys)
            .Build();

        // Act
        var actual = _authOptionsCreator.Create(route.AuthenticationOptions, globalConfig.AuthenticationOptions);

        // Assert
        actual.AllowedScopes.ShouldBe(expected.AllowedScopes);
        actual.AuthenticationProviderKey.ShouldBe(expected.AuthenticationProviderKey);
        actual.AuthenticationProviderKeys.ShouldBe(expected.AuthenticationProviderKeys);
    }

    private static FileRoute CreateFileRoute(List<string> allowedScopes, string authProviderKey, string[] authProviderKeys) => new()
    {
        AuthenticationOptions = new FileAuthenticationOptions
        {
            AllowedScopes = allowedScopes,
            AuthenticationProviderKey = authProviderKey,
            AuthenticationProviderKeys = authProviderKeys,
        },
    };

    private static FileGlobalConfiguration CreateGlobalConfiguration(List<string> allowedScopes, string authProviderKey, string[] authProviderKeys) => new()
    {
        AuthenticationOptions = new FileAuthenticationOptions
        {
            AllowedScopes = allowedScopes,
            AuthenticationProviderKey = authProviderKey,
            AuthenticationProviderKeys = authProviderKeys,
        },
    };
}
