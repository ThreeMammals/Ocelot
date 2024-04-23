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
        var actual = _creator.Create(null);

        // Assert
        Assert.NotNull(actual);
    }

    [Fact]
    public void Create_AuthenticationOptionsObjIsNull_IsAuthenticatedIsFalse()
    {
        // Arrange
        var route = new FileRoute { AuthenticationOptions = null };

        // Act
        var actual = _creator.Create(route);

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
        var actual = _creator.Create(route);

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

        // Act
        var actual = _creator.Create(route);

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
        var actual = _creator.Create(route);

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
        var actual = _creator.Create(route);

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
        var route = new FileRoute
        {
            RateLimitOptions = new FileRateLimitRule
            {
                EnableRateLimiting = true,
            },
            AuthenticationOptions = new FileAuthenticationOptions
            {
                AuthenticationProviderKey = !isAuthenticationProviderKeys ? "Test" : null,
                AuthenticationProviderKeys = isAuthenticationProviderKeys ?
                    new string[] { string.Empty, "Test #1" } : null,
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
        var expected = new RouteOptionsBuilder()
            .WithIsAuthenticated(true)
            .WithIsAuthorized(true)
            .WithIsCached(true)
            .WithRateLimiting(true)
            .WithUseServiceDiscovery(true)
            .Build();

        // Act
        var actual = _creator.Create(route);

        // Assert
        actual.IsAuthenticated.ShouldBe(expected.IsAuthenticated);
        actual.IsAuthorized.ShouldBe(expected.IsAuthorized);
        actual.IsCached.ShouldBe(expected.IsCached);
        actual.EnableRateLimiting.ShouldBe(expected.EnableRateLimiting);
        actual.UseServiceDiscovery.ShouldBe(expected.UseServiceDiscovery);
    }
}
