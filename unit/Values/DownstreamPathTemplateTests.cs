using Ocelot.Values;

namespace Ocelot.UnitTests.Values;

public class DownstreamPathTemplateTests : UnitTest
{
    [Fact]
    public void StringConstructor_ReturnsValue()
    {
        var template = new DownstreamPathTemplate("/api");

        Assert.Equal("/api", template.Value);
        Assert.Equal("/api", template.ToString());
    }

    [Fact]
    public void CachedFactory_IsEvaluatedOnce()
    {
        var calls = 0;
        var template = new DownstreamPathTemplate(() =>
        {
            calls++;
            return "/api";
        });

        Assert.Equal("/api", template.Value);
        Assert.Equal("/api", template.Value);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void NonCachedFactory_IsEvaluatedEachTime()
    {
        var calls = 0;
        var template = new DownstreamPathTemplate(() =>
        {
            calls++;
            return $"/api/{calls}";
        }, false);

        Assert.Equal("/api/1", template.Value);
        Assert.Equal("/api/2", template.Value);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void NullValue_IsRenderedAsEmptyString()
    {
        var template = new DownstreamPathTemplate((string)null);

        Assert.Null(template.Value);
        Assert.Empty(template.ToString());
    }

    [Fact]
    public void NullNonCachedFactory_FallsBackToBaseValue()
    {
        var template = new DownstreamPathTemplate(() => null, false);

        Assert.Null(template.Value);
    }
}
