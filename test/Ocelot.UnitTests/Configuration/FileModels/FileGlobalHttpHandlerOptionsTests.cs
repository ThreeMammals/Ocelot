using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration.FileModels;

[Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
[Trait("Feat", "2320")] // https://github.com/ThreeMammals/Ocelot/issues/2320
public class FileGlobalHttpHandlerOptionsTests
{
    [Fact]
    public void Ctor_Default()
    {
        // Arrange, Act
        FileGlobalHttpHandlerOptions actual = new();

        // Assert
        Assert.Null(actual.RouteKeys);
        Assert.Null(actual.UseTracing);
    }

    [Fact]
    public void Ctor_FileHttpHandlerOptions()
    {
        // Arrange
        FileHttpHandlerOptions from = new()
        {
            AllowAutoRedirect = true,
            MaxConnectionsPerServer = 111,
            PooledConnectionLifetimeSeconds = 222,
            UseCookieContainer = true,
            UseProxy = true,
            UseTracing = true,
        };

        // Act
        FileGlobalHttpHandlerOptions actual = new(from);

        // Assert
        Assert.Null(actual.RouteKeys);
        Assert.False(ReferenceEquals(from, actual));
        Assert.Equivalent(from, actual);
        Assert.True(actual.AllowAutoRedirect);
        Assert.Equal(111, actual.MaxConnectionsPerServer);
        Assert.Equal(222, actual.PooledConnectionLifetimeSeconds);
        Assert.True(actual.UseCookieContainer);
        Assert.True(actual.UseProxy);
        Assert.True(actual.UseTracing);
    }
}
