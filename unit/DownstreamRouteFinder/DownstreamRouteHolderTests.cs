using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;

namespace Ocelot.UnitTests.DownstreamRouteFinder;

public class DownstreamRouteHolderTests
{
    [Fact]
    public void Ctor()
    {
        // Arrange, Act
        DownstreamRouteHolder holder = new();

        Assert.Null(holder.Route);
        Assert.Null(holder.TemplatePlaceholderNameAndValues);
    }

    [Fact]
    public void Ctor_List_Route()
    {
        // Arrange
        Route route = new();
        List<PlaceholderNameAndValue> placeholders = new();

        // Act
        DownstreamRouteHolder holder = new(placeholders, route);

        // Assert
        Assert.Equal(route, holder.Route);
        Assert.Equal(placeholders, holder.TemplatePlaceholderNameAndValues);
    }
}
