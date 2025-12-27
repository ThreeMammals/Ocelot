using Ocelot.Cache;

namespace Ocelot.UnitTests.Cache;

public class CachedResponseTests
{
    [Fact]
    public void Ctor()
    {
        // Arrange
        Dictionary<string, IEnumerable<string>> headers = new()
        {
            { "header", ["headerValue"] },
        };

        // Act
        CachedResponse actual = new(HttpStatusCode.Created, headers, "body", headers, "reasonPhrase");

        // Assert
        Assert.Equal(HttpStatusCode.Created, actual.StatusCode);
        Assert.NotEmpty(actual.Headers);
        Assert.Contains("header", actual.Headers);
        Assert.NotEmpty(actual.ContentHeaders);
        Assert.Contains("header", actual.ContentHeaders);
        Assert.Equal("body", actual.Body);
        Assert.Equal("reasonPhrase", actual.ReasonPhrase);
    }

    [Fact]
    public void Ctor_Defaulting()
    {
        // Arrange, Act
        CachedResponse actual = new(HttpStatusCode.NotFound, null, null, null, "reasonPhrase");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, actual.StatusCode);
        Assert.Empty(actual.Headers);
        Assert.Empty(actual.ContentHeaders);
        Assert.Empty(actual.Body);
        Assert.Equal("reasonPhrase", actual.ReasonPhrase);
    }
}
