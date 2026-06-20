using Ocelot.DownstreamRouteFinder.UrlMatcher;

namespace Ocelot.UnitTests.DownstreamRouteFinder.UrlMatcher;

public sealed class PlaceholderNameAndValueTests
{
    [Fact]
    public void Key()
    {
        // Arrange
        PlaceholderNameAndValue placeholder = new("{test}", "testing");

        // Act
        var actual = placeholder.Key;

        // Assert
        Assert.Equal("testing", placeholder.Value);
        Assert.Equal("{test}", placeholder.Name);
        Assert.Equal("test", actual);
    }

    [Fact]
    public void ToString_Override()
    {
        // Arrange
        PlaceholderNameAndValue placeholder = new("{test}", "testing");

        // Act
        var actual = placeholder.ToString();

        // Assert
        Assert.Equal("testing", placeholder.Value);
        Assert.Equal("{test}", placeholder.Name);
        Assert.Equal("[{test}=testing]", actual);
    }
}
