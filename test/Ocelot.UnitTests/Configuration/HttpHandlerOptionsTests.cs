using Ocelot.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class HttpHandlerOptionsTests
{
    [Fact]
    public void Ctor()
    {
        // Arrange, Act
        HttpHandlerOptions actual = new();

        // Assert
        Assert.Equal(int.MaxValue, actual.MaxConnectionsPerServer);
        Assert.Equal(120, actual.PooledConnectionLifeTime.TotalSeconds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ctor_FileHttpHandlerOptions(bool isNull)
    {
        // Arrange
        bool? nullBool() => isNull ? null : true;
        int? nullInt() => isNull ? null : 123;
        FileHttpHandlerOptions from = new()
        {
            AllowAutoRedirect = nullBool(),
            MaxConnectionsPerServer = nullInt(),
            PooledConnectionLifetimeSeconds = nullInt(),
            UseCookieContainer = nullBool(),
            UseProxy = nullBool(),
            UseTracing = nullBool(),
        };

        // Act
        HttpHandlerOptions actual = new(from);

        // Assert
        bool expectedBool = !isNull;
        Assert.Equal(expectedBool, actual.AllowAutoRedirect);
        Assert.Equal(isNull ? int.MaxValue : 123, actual.MaxConnectionsPerServer);
        Assert.Equal(isNull ? 120 : 123, (int)actual.PooledConnectionLifeTime.TotalSeconds);
        Assert.Equal(expectedBool, actual.UseCookieContainer);
        Assert.Equal(expectedBool, actual.UseProxy);
        Assert.Equal(expectedBool, actual.UseTracing);
    }

    [Fact]
    public void Ctor_FileHttpHandlerOptions_MaxConnectionsPerServer()
    {
        // Arrange
        FileHttpHandlerOptions from = new()
        {
            MaxConnectionsPerServer = null,
        };

        // Act, Assert
        HttpHandlerOptions actual = new(from);
        Assert.Equal(int.MaxValue, actual.MaxConnectionsPerServer);

        from.MaxConnectionsPerServer = 0;
        actual = new(from);
        Assert.Equal(int.MaxValue, actual.MaxConnectionsPerServer);

        from.MaxConnectionsPerServer = 111;
        actual = new(from);
        Assert.Equal(111, actual.MaxConnectionsPerServer);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, null, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void Ctor_FileHttpHandlerOptions_bool(bool useTracing, bool? fromUseTracing, bool expected)
    {
        // Arrange
        FileHttpHandlerOptions from = new()
        {
            MaxConnectionsPerServer = 333,
            UseTracing = fromUseTracing,
        };

        // Act, Assert
        HttpHandlerOptions actual = new(from, useTracing);

        Assert.Equal(333, actual.MaxConnectionsPerServer);
        Assert.Equal(expected, actual.UseTracing);
    }
}
