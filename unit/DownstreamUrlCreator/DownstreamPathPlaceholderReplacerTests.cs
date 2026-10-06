using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator;
using Route = Ocelot.Configuration.Route;

namespace Ocelot.UnitTests.DownstreamUrlCreator;

public class DownstreamPathPlaceholderReplacerTests : UnitTest
{
    private readonly DownstreamPathPlaceholderReplacer _replacer = new();

    [Fact]
    public void Can_replace_no_template_variables()
    {
        // Arrange
        var route = GivenRoute();

        // Act
        var dsPath = _replacer.Replace(route.DownstreamRoute[0].DownstreamPathTemplate.Value, route.TemplatePlaceholderNameAndValues);

        // Assert
        dsPath.Value.ShouldBe(string.Empty);
    }

    [Fact]
    public void Can_replace_no_template_variables_with_slash()
    {
        // Arrange
        var route = GivenRoute("/");

        // Act
        var dsPath = _replacer.Replace(route.DownstreamRoute[0].DownstreamPathTemplate.Value, route.TemplatePlaceholderNameAndValues);

        // Assert
        dsPath.Value.ShouldBe("/");
    }

    [Fact]
    public void Can_replace_url_no_slash()
    {
        // Arrange
        var route = GivenRoute("api");

        // Act
        var dsPath = _replacer.Replace(route.DownstreamRoute[0].DownstreamPathTemplate.Value, route.TemplatePlaceholderNameAndValues);

        // Assert
        dsPath.Value.ShouldBe("api");
    }

    [Fact]
    public void Can_replace_url_one_slash()
    {
        // Arrange
        var route = GivenRoute("api/");

        // Act
        var dsPath = _replacer.Replace(route.DownstreamRoute[0].DownstreamPathTemplate.Value, route.TemplatePlaceholderNameAndValues);

        // Assert
        dsPath.Value.ShouldBe("api/");
    }

    [Fact]
    public void Can_replace_url_multiple_slash()
    {
        // Arrange
        var route = GivenRoute("api/product/products/");

        // Act
        var dsPath = _replacer.Replace(route.DownstreamRoute[0].DownstreamPathTemplate.Value, route.TemplatePlaceholderNameAndValues);

        // Assert
        dsPath.Value.ShouldBe("api/product/products/");
    }

    [Fact]
    public void Can_replace_url_one_template_variable()
    {
        // Arrange
        var route = GivenRoute("productservice/products/{productId}/",
            [
                new("{productId}", "1")
            ]);

        // Act
        var dsPath = _replacer.Replace(route.DownstreamRoute[0].DownstreamPathTemplate.Value, route.TemplatePlaceholderNameAndValues);

        // Assert
        dsPath.Value.ShouldBe("productservice/products/1/");
    }

    [Fact]
    public void Can_replace_url_one_template_variable_with_path_after()
    {
        // Arrange
        var route = GivenRoute("productservice/products/{productId}/variants",
            [
                new("{productId}", "1")
            ]);

        // Act
        var dsPath = _replacer.Replace(route.DownstreamRoute[0].DownstreamPathTemplate.Value, route.TemplatePlaceholderNameAndValues);

        // Assert
        dsPath.Value.ShouldBe("productservice/products/1/variants");
    }

    [Fact]
    public void Can_replace_url_two_template_variable()
    {
        // Arrange
        var route = GivenRoute("productservice/products/{productId}/variants/{variantId}",
            [
                new("{productId}", "1"),
                new("{variantId}", "12"),
            ]);

        // Act
        var dsPath = _replacer.Replace(route.DownstreamRoute[0].DownstreamPathTemplate.Value, route.TemplatePlaceholderNameAndValues);

        // Assert
        dsPath.Value.ShouldBe("productservice/products/1/variants/12");
    }

    [Fact]
    public void Can_replace_url_three_template_variable()
    {
        // Arrange
        var route = GivenRoute("productservice/category/{categoryId}/products/{productId}/variants/{variantId}",
            [
                new("{productId}", "1"),
                new("{variantId}", "12"),
                new("{categoryId}", "34"),
            ]);

        // Act
        var dsPath = _replacer.Replace(route.DownstreamRoute[0].DownstreamPathTemplate.Value, route.TemplatePlaceholderNameAndValues);

        // Assert
        dsPath.Value.ShouldBe("productservice/category/34/products/1/variants/12");
    }

    private static Route GivenRoute(string downstream = null, List<PlaceholderNameAndValue> placeholders = null)
    {
        var route = GivenDownstreamRoute(downstream, HttpMethods.Get);
        return new(route, HttpMethod.Get, placeholders);
    }

    private static DownstreamRoute GivenDownstreamRoute(string downstream = null, string method = null)
        => new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate(downstream)
            .WithUpstreamHttpMethod([method ?? HttpMethods.Get])
            .Build();
}
