using Ocelot.RateLimiting;

namespace Ocelot.UnitTests.RateLimiting;

public class RateLimitingHeadersTests
{
    [Fact]
    public void Cctor_PropsInitialized()
    {
        // Arrange, Act, Assert
        Assert.Equal("Retry-After", RateLimitingHeaders.Retry_After);
        Assert.Equal("X-RateLimit-Limit", RateLimitingHeaders.X_RateLimit_Limit);
        Assert.Equal("X-RateLimit-Remaining", RateLimitingHeaders.X_RateLimit_Remaining);
        Assert.Equal("X-RateLimit-Reset", RateLimitingHeaders.X_RateLimit_Reset);
    }
}
