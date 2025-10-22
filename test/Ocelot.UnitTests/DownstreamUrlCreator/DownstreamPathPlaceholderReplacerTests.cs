using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator;

namespace Ocelot.UnitTests.DownstreamUrlCreator;

public class DownstreamPathPlaceholderReplacerTests : UnitTest
{
    private readonly DownstreamPathPlaceholderReplacer _replacer = new();

    [Fact]
    public void Can_replace_no_template_variables()
    {
        // Arrange
        var holder = new DownstreamRouteHolder(
                new List<PlaceholderNameAndValue>(),
                GivenRoute());

        // Act
        var result = _replacer.Replace(holder.Route.DownstreamRoute[0].DownstreamPathTemplate.Value, holder.TemplatePlaceholderNameAndValues);

        // Assert
        result.Data.Value.ShouldBe(string.Empty);
    }

    [Fact]
    public void Can_replace_no_template_variables_with_slash()
    {
        // Arrange
        var holder = new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            GivenRoute("/"));

        // Act
        var result = _replacer.Replace(holder.Route.DownstreamRoute[0].DownstreamPathTemplate.Value, holder.TemplatePlaceholderNameAndValues);

        // Assert
        result.Data.Value.ShouldBe("/");
    }

    [Fact]
    public void Can_replace_url_no_slash()
    {
        // Arrange
        var holder = new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            GivenRoute("api"));

        // Act
        var result = _replacer.Replace(holder.Route.DownstreamRoute[0].DownstreamPathTemplate.Value, holder.TemplatePlaceholderNameAndValues);

        // Assert
        result.Data.Value.ShouldBe("api");
    }

    [Fact]
    public void Can_replace_url_one_slash()
    {
        // Arrange
        var holder = new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            GivenRoute("api/"));

        // Act
        var result = _replacer.Replace(holder.Route.DownstreamRoute[0].DownstreamPathTemplate.Value, holder.TemplatePlaceholderNameAndValues);

        // Assert
        result.Data.Value.ShouldBe("api/");
    }

    [Fact]
    public void Can_replace_url_multiple_slash()
    {
        // Arrange
        var holder = new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            GivenRoute("api/product/products/"));

        // Act
        var result = _replacer.Replace(holder.Route.DownstreamRoute[0].DownstreamPathTemplate.Value, holder.TemplatePlaceholderNameAndValues);

        // Assert
        result.Data.Value.ShouldBe("api/product/products/");
    }

    [Fact]
    public void Can_replace_url_one_template_variable()
    {
        // Arrange
        var templateVariables = new List<PlaceholderNameAndValue>
        {
            new("{productId}", "1"),
        };
        var holder = new DownstreamRouteHolder(
            templateVariables,
            GivenRoute("productservice/products/{productId}/"));

        // Act
        var result = _replacer.Replace(holder.Route.DownstreamRoute[0].DownstreamPathTemplate.Value, holder.TemplatePlaceholderNameAndValues);

        // Assert
        result.Data.Value.ShouldBe("productservice/products/1/");
    }

    [Fact]
    public void Can_replace_url_one_template_variable_with_path_after()
    {
        // Arrange
        var templateVariables = new List<PlaceholderNameAndValue>
        {
            new("{productId}", "1"),
        };
        var holder = new DownstreamRouteHolder(
            templateVariables,
            GivenRoute("productservice/products/{productId}/variants"));

        // Act
        var result = _replacer.Replace(holder.Route.DownstreamRoute[0].DownstreamPathTemplate.Value, holder.TemplatePlaceholderNameAndValues);

        // Assert
        result.Data.Value.ShouldBe("productservice/products/1/variants");
    }

    [Fact]
    public void Can_replace_url_two_template_variable()
    {
        // Arrange
        var templateVariables = new List<PlaceholderNameAndValue>
        {
            new("{productId}", "1"),
            new("{variantId}", "12"),
        };
        var holder = new DownstreamRouteHolder(
            templateVariables,
            GivenRoute("productservice/products/{productId}/variants/{variantId}"));

        // Act
        var result = _replacer.Replace(holder.Route.DownstreamRoute[0].DownstreamPathTemplate.Value, holder.TemplatePlaceholderNameAndValues);

        // Assert
        result.Data.Value.ShouldBe("productservice/products/1/variants/12");
    }

    [Fact]
    public void Can_replace_url_three_template_variable()
    {
        // Arrange
        var templateVariables = new List<PlaceholderNameAndValue>
        {
            new("{productId}", "1"),
            new("{variantId}", "12"),
            new("{categoryId}", "34"),
        };
        var holder = new DownstreamRouteHolder(
            templateVariables,
            GivenRoute("productservice/category/{categoryId}/products/{productId}/variants/{variantId}"));

        // Act
        var result = _replacer.Replace(holder.Route.DownstreamRoute[0].DownstreamPathTemplate.Value, holder.TemplatePlaceholderNameAndValues);

        // Assert
        result.Data.Value.ShouldBe("productservice/category/34/products/1/variants/12");
    }

    private static Route GivenRoute(string downstream = null, string method = null)
    {
        var route = GivenDownstreamRoute(downstream, method);
        return new()
        {
            DownstreamRoute = [route],
            UpstreamHttpMethod = [method is null ? HttpMethod.Get : new(method)],
        };
    }

    private static DownstreamRoute GivenDownstreamRoute(string downstream = null, string method = null)
        => new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate(downstream)
            .WithUpstreamHttpMethod([method ?? HttpMethods.Get])
            .Build();
}
