using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.RateLimiting;

public class FileRateLimitingTests
{
    [Fact]
    public void FileRateLimitByAspNetRule_Ctor()
    {
        // Arrange, Act
        FileRateLimitByAspNetRule actual = new();

        // Assert
        Assert.Null(actual.Policy);
    }

    [Fact]
    public void FileRateLimitByIpRule_Ctor()
    {
        // Arrange, Act
        FileRateLimitByIpRule actual = new();

        // Assert
        Assert.Null(actual.IPWhitelist);
    }

    [Fact]
    public void FileRateLimitByMethodRule_Ctor()
    {
        // Arrange, Act
        FileRateLimitByMethodRule actual = new();

        // Assert
        Assert.Null(actual.Methods);
    }

    [Fact]
    public void FileRateLimiting_Ctor()
    {
        // Arrange, Act
        FileRateLimiting actual = new();

        // Assert
        Assert.Null(actual.ByHeader);
        Assert.Null(actual.ByMethod);
        Assert.Null(actual.ByIP);
        Assert.Null(actual.ByAspNet);
        Assert.Null(actual.Metadata);
    }
}
