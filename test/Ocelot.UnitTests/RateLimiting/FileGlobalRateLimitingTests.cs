using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.RateLimiting;

public class FileGlobalRateLimitingTests
{
    [Fact]
    public void FileGlobalRateLimit_Ctor()
    {
        // Arrange, Act
        FileGlobalRateLimit actual = new();

        // Assert
        Assert.Null(actual.Name);
        Assert.Null(actual.Pattern);
    }

    [Fact]
    public void FileGlobalRateLimitByAspNetRule_Ctor()
    {
        // Arrange, Act
        FileGlobalRateLimitByAspNetRule actual = new();

        // Assert
        Assert.Null(actual.RouteKeys);
    }

    [Fact]
    public void FileGlobalRateLimitByHeaderRule_Ctor()
    {
        // Arrange, Act
        FileGlobalRateLimitByHeaderRule actual = new();

        // Assert
        Assert.Null(actual.RouteKeys);
    }

    [Fact]
    public void FileGlobalRateLimitByIpRule_Ctor()
    {
        // Arrange, Act
        FileGlobalRateLimitByIpRule actual = new();

        // Assert
        Assert.Null(actual.RouteKeys);
    }

    [Fact]
    public void FileGlobalRateLimitByMethodRule_Ctor()
    {
        // Arrange, Act
        FileGlobalRateLimitByMethodRule actual = new();

        // Assert
        Assert.Null(actual.RouteKeys);
    }

    [Fact]
    public void FileGlobalRateLimiting_Ctor()
    {
        // Arrange, Act
        FileGlobalRateLimiting actual = new();

        // Assert
        Assert.Null(actual.ByHeader);
        Assert.Null(actual.ByMethod);
        Assert.Null(actual.ByIP);
        Assert.Null(actual.ByAspNet);
        Assert.Null(actual.Metadata);
    }
}
