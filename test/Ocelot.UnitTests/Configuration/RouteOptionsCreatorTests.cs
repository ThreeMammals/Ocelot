using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class RouteOptionsCreatorTests : UnitTest
{
    private readonly RouteOptionsCreator _creator;

    public RouteOptionsCreatorTests()
    {
        _creator = new RouteOptionsCreator();
    }

    [Fact]
    public void Create_ArgumentIsNull_OptionsObjIsCreated()
    {
        // Arrange, Act
        var actual = _creator.Create(null, null);

        // Assert
        Assert.NotNull(actual);
    }

    [Fact]
    public void Create_AuthenticationOptionsObjIsNull_IsAuthenticatedIsFalse()
    {
        // Arrange
        var route = new FileRoute { AuthenticationOptions = null };

        // Act
        var actual = _creator.Create(route, null);

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
        var actual = _creator.Create(route, null);

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
        var globalConfig = RouteOptionsCreatorTests.CreateGlobalConfiguration(null, null);

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
    }

    [Fact]
    public void Create_RateLimitOptionsObjIsNull_EnableRateLimitingIsFalse()
    {
        // Arrange
        var route = new FileRoute
        {
            RateLimitOptions = null,
        };

        // Act
        var actual = _creator.Create(route, null);

        // Assert
        Assert.NotNull(actual);
        Assert.False(actual.EnableRateLimiting);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_RouteOptions_HappyPath(bool isAuthenticationProviderKeys)
    {
        // Arrange
        var route = RouteOptionsCreatorTests.CreateFileRoute(!isAuthenticationProviderKeys ? "Test" : null,
                                    isAuthenticationProviderKeys ? new string[] { string.Empty, "Test #1" } : null,
                                    false);
        var globalConfig = RouteOptionsCreatorTests.CreateGlobalConfiguration(null, null);
        var expected = new RouteOptionsBuilder()
            .WithIsAuthenticated(true)
            .WithIsAuthorized(true)
            .WithIsCached(true)
            .WithRateLimiting(true)
            .WithUseServiceDiscovery(true)
            .Build();

        // Act
        var actual = _creator.Create(route, globalConfig);

        // Assert
        actual.IsAuthenticated.ShouldBe(expected.IsAuthenticated);
        actual.IsAuthorized.ShouldBe(expected.IsAuthorized);
        actual.IsCached.ShouldBe(expected.IsCached);
        actual.EnableRateLimiting.ShouldBe(expected.EnableRateLimiting);
        actual.UseServiceDiscovery.ShouldBe(expected.UseServiceDiscovery);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Create_ProviderKeyInGlobalConfig_ShouldSetIsAuthenticatedDependOnAllowAnonymous(bool globalConfigHasSingleProviderKey, bool allowAnonymous)
    {
        // Arrange
        var route = RouteOptionsCreatorTests.CreateFileRoute(null, null, allowAnonymous);
        var globalConfig = RouteOptionsCreatorTests.CreateGlobalConfiguration(globalConfigHasSingleProviderKey ? "key" : null,
                                                     globalConfigHasSingleProviderKey ? null : new string[] { "key1", "key2" });
        var expected = new RouteOptionsBuilder()
                .WithIsAuthenticated(!allowAnonymous)
                .WithIsAuthorized(true)
                .WithIsCached(true)
                .WithRateLimiting(true)
                .WithUseServiceDiscovery(true)
                .Build();

        // Act
        var actual = _creator.Create(route, globalConfig);

        // Assert
        actual.IsAuthenticated.ShouldBe(expected.IsAuthenticated);
        actual.IsAuthorized.ShouldBe(expected.IsAuthorized);
        actual.IsCached.ShouldBe(expected.IsCached);
        actual.EnableRateLimiting.ShouldBe(expected.EnableRateLimiting);
        actual.UseServiceDiscovery.ShouldBe(expected.UseServiceDiscovery);
    }

    private static FileRoute CreateFileRoute(string authProviderKey, string[] authProviderKeys, bool allowAnonymous) => new()
        {
            RateLimitOptions = new FileRateLimitRule
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
            AuthenticationOptions = new FileAuthenticationOptions
            {
                AuthenticationProviderKey = authProviderKey,
                AuthenticationProviderKeys = authProviderKeys,
            },
        };
}
