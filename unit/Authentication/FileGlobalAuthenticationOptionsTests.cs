using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Authentication;

[Trait("Feat", "585")]
[Trait("Feat", "2316")] // https://github.com/ThreeMammals/Ocelot/issues/2316
[Trait("PR", "2336")] // https://github.com/ThreeMammals/Ocelot/pull/2336
public class FileGlobalAuthenticationOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeBaseAndRouteKeys()
    {
        // Act
        var options = new FileGlobalAuthenticationOptions();

        // Assert
        Assert.NotNull(options);
        Assert.Null(options.RouteKeys); // not initialized by default
    }

    [Fact]
    public void Constructor_WithAuthScheme_ShouldSetAuthScheme()
    {
        // Arrange
        var authScheme = "TestScheme";

        // Act
        var options = new FileGlobalAuthenticationOptions(authScheme);

        // Assert
        Assert.NotNull(options);
        Assert.Single(options.AuthenticationProviderKeys, authScheme);
    }

    [Fact]
    public void Constructor_WithFileAuthenticationOptions_ShouldCopyValues()
    {
        // Arrange
        var from = new FileAuthenticationOptions()
        {
            AllowAnonymous = true,
            AllowedScopes = ["scope"],
            AuthenticationProviderKey = "key",
            AuthenticationProviderKeys = ["key1", "key2"],
        };

        // Act
        var options = new FileGlobalAuthenticationOptions(from);

        // Assert
        Assert.NotNull(options);
        Assert.Null(options.RouteKeys);
        FileAuthenticationOptions actual = (FileAuthenticationOptions)options;
        Assert.Equivalent(from, actual);
        Assert.Equal(from.AuthenticationProviderKey, options.AuthenticationProviderKey);
        Assert.Contains("key1", options.AuthenticationProviderKeys);
        Assert.Contains("key2", options.AuthenticationProviderKeys);
    }

    [Fact]
    public void RouteKeys_ShouldAllowSetAndGet()
    {
        // Arrange
        var options = new FileGlobalAuthenticationOptions();
        var keys = new HashSet<string> { "route1", "route2" };

        // Act
        options.RouteKeys = keys;

        // Assert
        Assert.NotNull(options.RouteKeys);
        Assert.Contains("route1", options.RouteKeys);
        Assert.Contains("route2", options.RouteKeys);
    }

    [Fact]
    public void ShouldImplementIRouteGroup()
    {
        // Act
        var options = new FileGlobalAuthenticationOptions();

        // Assert
        Assert.True(options is IRouteGroup);
    }
}
