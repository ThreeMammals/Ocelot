using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Middleware;

namespace Ocelot.UnitTests.Middleware;

public class HttpItemsExtensionsTests
{
    [Fact]
    public void UpsertGeneric_ShouldReplaceTheValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var route1 = new Route();
        context.Items.UpsertRoute(route1);

        // Act
        var route2 = new Route();
        context.Items.UpsertRoute(route2);
        var actual = context.Items.Route();

        // Assert
        Assert.NotSame(route1, actual);
        Assert.Same(route2, actual);
        var referenceEquality = Equals(actual, route2);
        Assert.True(referenceEquality);
    }
}
