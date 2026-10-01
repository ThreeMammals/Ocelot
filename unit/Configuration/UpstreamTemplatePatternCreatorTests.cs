using Ocelot.Cache;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Values;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ocelot.UnitTests.Configuration;

public class UpstreamTemplatePatternCreatorTests : UnitTest
{
    private readonly Mock<IOcelotCache<Regex>> _cache;
    private readonly UpstreamTemplatePatternCreator _creator;
    private const string MatchEverything = UpstreamTemplatePatternCreator.RegExMatchZeroOrMoreOfEverything;

    public UpstreamTemplatePatternCreatorTests()
    {
        _cache = new();
        _creator = new UpstreamTemplatePatternCreator(_cache.Object);
    }

    [Fact]
    public void Should_match_up_to_next_slash()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/v{apiVersion}/cards",
            Priority = 0,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe("^(?i)/api/v[^/]+/cards$");
        result.Priority.ShouldBe(0);
    }

    [Fact]
    public void Should_use_re_route_priority()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/orders/{catchAll}",
            Priority = 0,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe($"^(?i)/orders(?:|/{MatchEverything})$");
        result.Priority.ShouldBe(0);
    }

    [Fact]
    public void Should_use_zero_priority()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/{catchAll}",
            Priority = 1,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe("^/.*");
        result.Priority.ShouldBe(0);
    }

    [Fact]
    public void Should_set_upstream_template_pattern_to_ignore_case_sensitivity()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/PRODUCTS/{productId}",
            RouteIsCaseSensitive = false,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe($"^(?i)/PRODUCTS(?:|/{MatchEverything})$");
        result.Priority.ShouldBe(1);
    }

    [Fact]
    public void Should_match_forward_slash_or_no_forward_slash_if_template_end_with_forward_slash()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/PRODUCTS/",
            RouteIsCaseSensitive = false,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe("^(?i)/PRODUCTS(/|)$");
        result.Priority.ShouldBe(1);
    }

    [Fact]
    public void Should_set_upstream_template_pattern_to_respect_case_sensitivity()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/PRODUCTS/{productId}",
            RouteIsCaseSensitive = true,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe($"^/PRODUCTS(?:|/{MatchEverything})$");
        result.Priority.ShouldBe(1);
    }

    [Fact]
    public void Should_create_template_pattern_that_matches_anything_to_end_of_string()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/products/{productId}",
            RouteIsCaseSensitive = true,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe($"^/api/products(?:|/{MatchEverything})$");
        result.Priority.ShouldBe(1);
    }

    [Fact]
    public void Should_create_template_pattern_that_matches_more_than_one_placeholder()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/products/{productId}/variants/{variantId}",
            RouteIsCaseSensitive = true,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe($"^/api/products/[^/]+/variants(?:|/{MatchEverything})$");
        result.Priority.ShouldBe(1);
    }

    [Fact]
    public void Should_create_template_pattern_that_matches_more_than_one_placeholder_with_trailing_slash()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/products/{productId}/variants/{variantId}/",
            RouteIsCaseSensitive = true,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe("^/api/products/[^/]+/variants/[^/]+(/|)$");
        result.Priority.ShouldBe(1);
    }

    [Fact]
    public void Should_create_template_pattern_that_matches_to_end_of_string()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/",
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe("^/$");
        result.Priority.ShouldBe(1);
    }

    [Fact]
    public void Should_create_template_pattern_that_matches_to_end_of_string_when_slash_and_placeholder()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/{url}",
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe("^/.*");
        result.Priority.ShouldBe(0);
    }

    [Fact]
    public void Should_create_template_pattern_that_starts_with_placeholder_then_has_another_later()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/{productId}/products/variants/{variantId}/",
            RouteIsCaseSensitive = true,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe("^/[^/]+/products/variants/[^/]+(/|)$");
        result.Priority.ShouldBe(1);
    }

    [Fact]
    public void Should_create_template_pattern_that_matches_query_string()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe($@"^(?i)/api/subscriptions/[^/]+/updates(/$|/\?|\?|$)unitId={MatchEverything}$");
        result.Priority.ShouldBe(1);
    }

    [Fact]
    public void Should_create_template_pattern_that_matches_query_string_with_multiple_params()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}&productId={productId}",
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe($"^(?i)/api/subscriptions/[^/]+/updates(/$|/\\?|\\?|$)unitId={MatchEverything}&productId={MatchEverything}$");
        result.Priority.ShouldBe(1);
    }

    [Theory]
    [Trait("Bug", "2064")]
    [InlineData("/{tenantId}/products?{everything}", "/1/products/1", false)]
    [InlineData("/{tenantId}/products/{everything}", "/1/products/1", true)]
    public void Should_not_match_when_placeholder_appears_after_query_start(string urlPathTemplate, string requestPath, bool shouldMatch)
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = urlPathTemplate,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.ShouldMatchWithRegex(requestPath, shouldMatch);
    }

    [Theory]
    [Trait("Bug", "2132")]
    [InlineData("/api/v1/abc?{everything}", "/api/v1/abc2/apple", false)]
    [InlineData("/api/v1/abc2/{everything}", "/api/v1/abc2/apple", true)]
    public void Should_not_match_with_query_param_wildcard(string urlPathTemplate, string requestPath, bool shouldMatch)
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = urlPathTemplate,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.ShouldMatchWithRegex(requestPath, shouldMatch);
    }

    [Fact]
    [Trait("Feat", "1348")]
    [Trait("Bug", "2246")]
    public void GetRegex_NoKey_ReturnsNull()
    {
        // Act
        var actual = GetRegex.Invoke(_creator, new object[] { string.Empty });

        // Assert
        actual.ShouldBeNull();
    }

    [Fact]
    [Trait("Feat", "1348")]
    [Trait("Bug", "2246")]
    public void CreateTemplate_PatternProperty_NullChecks()
    {
        // Act
        string nullTemplate = null;
        var actual = CreateTemplate.Invoke(_creator, new object[] { nullTemplate, 0, false, null }) as UpstreamPathTemplate;

        // Assert
        actual.ShouldNotBeNull();
        actual.Pattern.ShouldNotBeNull().ToString().ShouldBe("$^");
    }

    [Theory]
    [InlineData("/v3/Orders/$query", @"^(?i)/v3/Orders/\$query$")]
    [InlineData("/api/{id}/$count", @"^(?i)/api/[^/]+/\$count$")]
    [InlineData("/odata/Products(1)", @"^(?i)/odata/Products\(1\)$")]
    [InlineData("/odata/Products({key})", @"^(?i)/odata/Products\(.*\)$")]
    [InlineData("/odata/Orders?$filter={filter}", @"^(?i)/odata/Orders(/$|/\?|\?|$)\$filter=.*$")]
    [InlineData("/odata/Orders/?$filter={filter}", @"^(?i)/odata/Orders(/$|/\?|\?|$)\$filter=.*$")]
    [InlineData("/api/v1.0/items", @"^(?i)/api/v1\.0/items$")]
    [InlineData("/files/.well-known", @"^(?i)/files/\.well-known$")]
    [InlineData("/api/a+b", @"^(?i)/api/a\+b$")]
    [InlineData("/api/(v1", @"^(?i)/api/\(v1$")]
    [InlineData("/api/v1)", @"^(?i)/api/v1\)$")]
    [InlineData("/api/[v1", @"^(?i)/api/\[v1$")]
    [Trait("Bug", "2143")] // https://github.com/ThreeMammals/Ocelot/issues/2143
    public void Should_escape_special_characters_of_template(string urlPathTemplate, string expectedTemplate)
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = urlPathTemplate,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.Template.ShouldBe(expectedTemplate);
        result.Priority.ShouldBe(1);
    }

    [Theory]
    [InlineData("/v3/Orders/$query", "/v3/Orders/$query", true)]
    [InlineData("/v3/Orders/$query", "/v3/Orders/query", false)]
    [InlineData("/odata/$metadata", "/odata/$metadata", true)]
    [InlineData("/api/{id}/$count", "/api/5/$count", true)]
    [InlineData("/odata/Products(1)", "/odata/Products(1)", true)]
    [InlineData("/odata/Products({key})", "/odata/Products(1)", true)]
    [InlineData("/odata/Products({key})", "/odata/Products1", false)]
    [InlineData("/odata/Products({key})?$select={select}", "/odata/Products(1)?$select=Name", true)]
    [InlineData("/api/a+b", "/api/a+b", true)]
    [InlineData("/api/a+b", "/api/aab", false)]
    [InlineData("/api/v1.0/items", "/api/v1.0/items", true)]
    [InlineData("/api/v1.0/items", "/api/v1X0/items", false)]
    [InlineData("/api/v1.0/{everything}", "/api/v1.0", true)]
    [InlineData("/files/.well-known", "/files/.well-known", true)]
    [InlineData("/files/.well-known", "/files", false)]
    [InlineData("/api/items|admin", "/api/admin", false)]
    [Trait("Bug", "2143")] // https://github.com/ThreeMammals/Ocelot/issues/2143
    public void Should_match_special_characters_of_template_literally(string urlPathTemplate, string requestPath, bool shouldMatch)
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = urlPathTemplate,
        };

        // Act
        var result = _creator.Create(fileRoute);

        // Assert
        result.ShouldMatchWithRegex(requestPath, shouldMatch);
    }

    private static Type Me { get; } = typeof(UpstreamTemplatePatternCreator);
    private static MethodInfo GetRegex { get; } = Me.GetMethod(nameof(GetRegex), BindingFlags.NonPublic | BindingFlags.Instance);
    private static MethodInfo CreateTemplate { get; } = Me.GetMethod(nameof(CreateTemplate), BindingFlags.NonPublic | BindingFlags.Instance);
}

internal static class UpstreamPathTemplateExtensions
{
    public static void ShouldMatchWithRegex(this UpstreamPathTemplate actual, string requestPath, bool shouldMatch)
    {
        var match = Regex.Match(requestPath, actual.Template);
        Assert.Equal(shouldMatch, match.Success);
    }
}
