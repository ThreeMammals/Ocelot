using Microsoft.AspNetCore.Http;
using Ocelot.RateLimiting;
using System.Collections.Generic;
using System.Reflection;

namespace Ocelot.UnitTests.RateLimiting;

public class RateLimitHeadersTests
{
    [Fact]
    public void Ctor_Created()
    {
        // Arrange
        HttpContext ctx = new DefaultHttpContext();
        long limit = 3, remaining = 2;
        DateTime today = DateTime.Today;

        // Act
        RateLimitHeaders actual = new(ctx, limit, remaining, today);

        // Assert
        Assert.Equal(ctx, actual.Context);
        Assert.Equal(limit, actual.Limit);
        Assert.Equal(remaining, actual.Remaining);
        Assert.Equal(today, actual.Reset);
    }

    [Fact]
    public void Ctor_Parameterless()
    {
        // Arrange
        ConstructorInfo ctor = typeof(RateLimitHeaders).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null, types: Type.EmptyTypes, modifiers: null);
        Assert.NotNull(ctor);
        Assert.False(ctor.IsPublic);

        // Act
        RateLimitHeaders actual = ctor.Invoke(null) as RateLimitHeaders;

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(default, actual.Context);
        Assert.Equal(default, actual.Limit);
        Assert.Equal(default, actual.Remaining);
        Assert.Equal(default, actual.Reset);
    }
}
