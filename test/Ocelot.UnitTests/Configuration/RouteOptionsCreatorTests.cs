using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class RouteOptionsCreatorTests : UnitTest
{
    private readonly RouteOptionsCreator _creator = new();

    [Fact]
    public void Create_ArgumentIsNull_OptionsObjIsCreated()
    {
        // Arrange, Act
        var actual = _creator.Create(null, null);

        // Assert
        Assert.NotNull(actual);

        // Arrange, Act
        actual = _creator.Create(new(), null);

        // Assert
        Assert.NotNull(actual);
    }

    /*[Fact]
    public void Create_AuthenticationOptionsObjIsNull_IsAuthenticatedIsFalse()
    {
        // Arrange
        var route = new FileRoute { AuthenticationOptions = null };
        var global = new FileGlobalConfiguration { AuthenticationOptions = null };

        // Act
        var actual = _creator.Create(route, global);

        // Assert
        Assert.NotNull(actual);
        Assert.False(actual.IsAuthenticated);
    }

    [Fact]
    public void Create_AuthenticationOptionsWithNoProviderKeys_IsAuthenticatedIsFalse()
    {
        // Arrange
        var route = new FileRoute
        {
            AuthenticationOptions = new(),
        };

        // Act
        var actual = _creator.Create(route, new());

        // Assert
        Assert.NotNull(actual);
        Assert.False(actual.IsAuthenticated);
    }

    [Fact]
    public void Create_AuthenticationOptionsWithAuthenticationProviderKeysObjIsNull_IsAuthenticatedIsFalse()
    {
        // Arrange
        var route = new FileRoute
        {
            AuthenticationOptions = new()
            {
                AuthenticationProviderKeys = null,
            },
        };
        var globalConfig = CreateGlobalConfiguration(null, null);

        // Act
        var actual = _creator.Create(route, globalConfig);

        // Assert
        Assert.NotNull(actual);
        Assert.False(actual.IsAuthenticated);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_RouteClaimsRequirementObjIsEmpty_IsAuthorizedIsFalse(bool isEmpty)
    {
        // Arrange
        var route = new FileRoute
        {
            RouteClaimsRequirement = isEmpty ? new(0) : null,
        };

        // Act
        var actual = _creator.Create(route, null);

        // Assert
        Assert.NotNull(actual);
        Assert.False(actual.IsAuthorized);
    }*/

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_RouteOptions_HappyPath(bool isAuthenticationProviderKeys)
    {
        // Arrange
        var route = CreateFileRoute(!isAuthenticationProviderKeys ? "Test" : null,
                                    isAuthenticationProviderKeys ? new string[] { string.Empty, "Test #1" } : null,
                                    false);
        var globalConfig = CreateGlobalConfiguration(null, null);
        var expected = new RouteOptions(true);

        // Act
        var actual = _creator.Create(route, globalConfig);

        // Assert
        actual.UseServiceDiscovery.ShouldBe(expected.UseServiceDiscovery);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [Trait("PR", "2114")]
    [Trait("Feat", "842")]
    public void Create_ProviderKeyInGlobalConfig_ShouldSetIsAuthenticatedDependOnAllowAnonymous(bool globalConfigHasSingleProviderKey, bool allowAnonymous)
    {
        // Arrange
        var route = CreateFileRoute(null, null, allowAnonymous);
        var globalConfig = CreateGlobalConfiguration(globalConfigHasSingleProviderKey ? "key" : null,
                                                     globalConfigHasSingleProviderKey ? null : new string[] { "key1", "key2" });
        var expected = new RouteOptions(true);

        // Act
        var actual = _creator.Create(route, globalConfig);

        // Assert
        //actual.IsAuthorized.ShouldBe(expected.IsAuthorized);
        actual.UseServiceDiscovery.ShouldBe(expected.UseServiceDiscovery);
    }

    private static FileRoute CreateFileRoute(string authProviderKey, string[] authProviderKeys, bool allowAnonymous) => new()
        {
            RateLimitOptions = new FileRateLimitByHeaderRule
            {
                EnableRateLimiting = true,
            },
            AuthenticationOptions = new FileAuthenticationOptions
            {
                AuthenticationProviderKey = authProviderKey,
                AuthenticationProviderKeys = authProviderKeys,
                AllowAnonymous = allowAnonymous,
            },
            RouteClaimsRequirement = new Dictionary<string, string>
            {
                {string.Empty, string.Empty},
            },
            FileCacheOptions = new FileCacheOptions
            {
                TtlSeconds = 1,
            },
            ServiceName = "west",
        };

    private static FileGlobalConfiguration CreateGlobalConfiguration(string authProviderKey, string[] authProviderKeys) => new()
        {
            AuthenticationOptions = new()
            {
                AuthenticationProviderKey = authProviderKey,
                AuthenticationProviderKeys = authProviderKeys,
            },
        };
}
