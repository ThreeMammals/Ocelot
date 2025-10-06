using Ocelot.Configuration;
using Ocelot.Configuration.Builder;

namespace Ocelot.UnitTests.Configuration;

public class RouteTests
{
    [Fact]
    public void Ctor()
    {
        // Arrange, Act
        Route r = new();

        // Assert
        Assert.NotNull(r.DownstreamRoute);
        Assert.Empty(r.DownstreamRoute);

        Assert.False(r.IsDynamic);
        Assert.Null(r.Aggregator);
        Assert.NotNull(r.DownstreamRoute);
        Assert.Null(r.DownstreamRouteConfig);
        Assert.Null(r.UpstreamHeaderTemplates);
        Assert.Null(r.UpstreamHost);
        Assert.Null(r.UpstreamHttpMethod);
        Assert.Null(r.UpstreamTemplatePattern);
    }

    [Fact]
    public void Ctor_Boolean()
    {
        // Arrange, Act
        Route r1 = new(true),
            r2 = new(false);

        Assert.True(r1.IsDynamic);
        Assert.False(r2.IsDynamic);
        Assert.Empty(r1.DownstreamRoute);
        Assert.Empty(r2.DownstreamRoute);
    }

    [Fact]
    public void Ctor_Boolean_DownstreamRoute()
    {
        // Arrange
        DownstreamRoute route = new DownstreamRouteBuilder().Build();

        // Act
        Route r = new(true, route);

        Assert.True(r.IsDynamic);
        Assert.NotEmpty(r.DownstreamRoute);
        Assert.Equal(route, r.DownstreamRoute[0]);
    }

    [Fact]
    public void Ctor_DownstreamRoute()
    {
        // Arrange
        DownstreamRoute route = new DownstreamRouteBuilder().Build();

        // Act
        Route r = new(route);

        Assert.False(r.IsDynamic);
        Assert.NotEmpty(r.DownstreamRoute);
        Assert.Equal(route, r.DownstreamRoute[0]);
    }

    [Fact]
    public void Ctor_DownstreamRoute_HttpMethod()
    {
        // Arrange
        DownstreamRoute route = new DownstreamRouteBuilder().Build();
        HttpMethod method = HttpMethod.Connect;

        // Act
        Route r = new(route, method);

        Assert.NotEmpty(r.DownstreamRoute);
        Assert.Equal(route, r.DownstreamRoute[0]);
        Assert.NotEmpty(r.UpstreamHttpMethod);
        Assert.Equal(method, r.UpstreamHttpMethod.First());
    }
}
