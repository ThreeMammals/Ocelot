using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Values;

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
        Assert.NotNull(r.UpstreamHttpMethod);
        Assert.NotNull(r.TemplatePlaceholderNameAndValues);
        Assert.Empty(r.DownstreamRoute);
        Assert.Empty(r.UpstreamHttpMethod);
        Assert.Empty(r.TemplatePlaceholderNameAndValues);

        Assert.False(r.IsDynamic);
        Assert.Null(r.Aggregator);
        Assert.Null(r.DownstreamRouteConfig);
        Assert.Null(r.UpstreamHeaderTemplates);
        Assert.Null(r.UpstreamHost);
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

    [Fact]
    [Trait("Bug", "2428")] // https://github.com/ThreeMammals/Ocelot/issues/2428
    [Trait("PR", "2429")] // https://github.com/ThreeMammals/Ocelot/pull/2429
    public void Ctor_Route_List()
    {
        // Arrange
        Route from = new(true, new DownstreamRouteBuilder().Build())
        {
            Aggregator = "aggregator",
            DownstreamRouteConfig = [],
            UpstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>(),
            UpstreamHost = "host",
            UpstreamHttpMethod = [HttpMethod.Get],
            UpstreamTemplatePattern = new("/{id}", 1, false, "/{id}"),
        };
        List<PlaceholderNameAndValue> placeholders = [new("{id}", "1")];

        // Act
        Route r = new(from, placeholders);

        // Assert
        Assert.True(r.IsDynamic);
        Assert.Equal(from.Aggregator, r.Aggregator);
        Assert.Same(from.DownstreamRoute, r.DownstreamRoute);
        Assert.Same(from.DownstreamRouteConfig, r.DownstreamRouteConfig);
        Assert.Same(placeholders, r.TemplatePlaceholderNameAndValues);
        Assert.Same(from.UpstreamHeaderTemplates, r.UpstreamHeaderTemplates);
        Assert.Equal(from.UpstreamHost, r.UpstreamHost);
        Assert.Same(from.UpstreamHttpMethod, r.UpstreamHttpMethod);
        Assert.Same(from.UpstreamTemplatePattern, r.UpstreamTemplatePattern);
        Assert.Empty(from.TemplatePlaceholderNameAndValues);
    }
}
