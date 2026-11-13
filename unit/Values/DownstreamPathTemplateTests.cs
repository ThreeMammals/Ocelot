using Ocelot.Values;

namespace Ocelot.UnitTests.Values;

public class DownstreamPathTemplateTests : UnitTest
{
    [Fact]
    [Trait("PR", "1198")]
    public void StringConstructor_ReturnsValue()
    {
        // Act
        var template = new DownstreamPathTemplate("/api");

        // Assert
        Assert.Equal("/api", template.Value);
        Assert.Equal("/api", template.ToString());
    }

    [Fact]
    [Trait("PR", "1198")]
    public void CachedFactory_IsEvaluatedOnce()
    {
        // Arrange
        var calls = 0;
        var template = new DownstreamPathTemplate(() =>
        {
            calls++;
            return "/api";
        });

        // Act
        var first = template.Value;
        var second = template.Value;

        // Assert
        Assert.Equal("/api", first);
        Assert.Equal("/api", second);
        Assert.Equal(1, calls);
    }

    [Fact]
    [Trait("PR", "1198")]
    public void NonCachedFactory_IsEvaluatedEachTime()
    {
        // Arrange
        var calls = 0;
        var template = new DownstreamPathTemplate(() =>
        {
            calls++;
            return $"/api/{calls}";
        }, false);

        // Act
        var first = template.Value;
        var second = template.Value;

        // Assert
        Assert.Equal("/api/1", first);
        Assert.Equal("/api/2", second);
        Assert.Equal(2, calls);
    }

    [Fact]
    [Trait("PR", "1198")]
    public void NullValue_IsRenderedAsEmptyString()
    {
        // Act
        var template = new DownstreamPathTemplate((string)null);

        // Assert
        Assert.Null(template.Value);
        Assert.Empty(template.ToString());
    }

    [Fact]
    [Trait("PR", "1198")]
    public void NullNonCachedFactory_FallsBackToBaseValue()
    {
        // Act
        var template = new DownstreamPathTemplate(() => null, false);

        // Assert
        Assert.Null(template.Value);
    }
}
