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
        // Arrange, Act, Assert
        FileGlobalRateLimitByHeaderRule actual = new();
        Assert.Null(actual.RouteKeys);

        // Arrange
        FileRateLimitByHeaderRule from = new()
        {
            ClientIdHeader = "1",
            ClientWhitelist = ["2"],
            DisableRateLimitHeaders = true,
            HttpStatusCode = 4,
            QuotaExceededMessage = "5",
            RateLimitCounterPrefix = "6",
        };

        // Act
        FileGlobalRateLimitByHeaderRule actualG = new(from);

        // Assert
        Assert.False(ReferenceEquals(from, actualG));
        Assert.Equivalent(from, actualG);
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
