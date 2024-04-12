using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class AuthenticationOptionsCreatorTests
{
    private readonly AuthenticationOptionsCreator _authOptionsCreator;

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
        var actual = _authOptionsCreator.Create(route);

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
        string authenticationProviderKey = !isAuthenticationProviderKeys ? "Test" : null;
        string[] authenticationProviderKeys = isAuthenticationProviderKeys ?
            new string[] { "Test #1", "Test #2" } : null;
        var fileRoute = new FileRoute()
        {
            AuthenticationOptions = new FileAuthenticationOptions
            {
                AllowedScopes = new() { "cheese" },
                AuthenticationProviderKey = authenticationProviderKey,
                AuthenticationProviderKeys = authenticationProviderKeys,
            },
        };
        var expected = new AuthenticationOptionsBuilder()
            .WithAllowedScopes(fileRoute.AuthenticationOptions?.AllowedScopes)
            .WithAuthenticationProviderKey(authenticationProviderKey)
            .WithAuthenticationProviderKeys(authenticationProviderKeys)
            .Build();

        // Act
        var actual = _authOptionsCreator.Create(fileRoute);

        // Assert
        actual.AllowedScopes.ShouldBe(expected.AllowedScopes);
        actual.AuthenticationProviderKey.ShouldBe(expected.AuthenticationProviderKey);
        actual.AuthenticationProviderKeys.ShouldBe(expected.AuthenticationProviderKeys);
    }
}
