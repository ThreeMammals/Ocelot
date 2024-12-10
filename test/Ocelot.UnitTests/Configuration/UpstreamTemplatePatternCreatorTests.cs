using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Values;
using System.Text.RegularExpressions;

namespace Ocelot.UnitTests.Configuration;

public class UpstreamTemplatePatternCreatorTests : UnitTest
{
    private FileRoute _fileRoute;
    private readonly UpstreamTemplatePatternCreator _creator;
    private UpstreamPathTemplate _result;
    private const string MatchEverything = UpstreamTemplatePatternCreator.RegExMatchZeroOrMoreOfEverything;

    public UpstreamTemplatePatternCreatorTests()
    {
        _creator = new UpstreamTemplatePatternCreator();
    }

    [Fact]
    public void should_match_up_to_next_slash()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/v{apiVersion}/cards",
            Priority = 0,
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned("^(?i)/api/v[^/]+/cards$"))
            .And(x => ThenThePriorityIs(0))
            .BDDfy();
    }

    [Fact]
    public void should_use_re_route_priority()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/orders/{catchAll}",
            Priority = 0,
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned($"^(?i)/orders(?:|/{MatchEverything})$"))
            .And(x => ThenThePriorityIs(0))
            .BDDfy();
    }

    [Fact]
    public void should_use_zero_priority()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/{catchAll}",
            Priority = 1,
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned("^/.*"))
            .And(x => ThenThePriorityIs(0))
            .BDDfy();
    }

    [Fact]
    public void should_set_upstream_template_pattern_to_ignore_case_sensitivity()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/PRODUCTS/{productId}",
            RouteIsCaseSensitive = false,
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned($"^(?i)/PRODUCTS(?:|/{MatchEverything})$"))
            .And(x => ThenThePriorityIs(1))
            .BDDfy();
    }

    [Fact]
    public void should_match_forward_slash_or_no_forward_slash_if_template_end_with_forward_slash()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/PRODUCTS/",
            RouteIsCaseSensitive = false,
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned("^(?i)/PRODUCTS(/|)$"))
            .And(x => ThenThePriorityIs(1))
            .BDDfy();
    }

    [Fact]
    public void should_set_upstream_template_pattern_to_respect_case_sensitivity()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/PRODUCTS/{productId}",
            RouteIsCaseSensitive = true,
        };
        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned($"^/PRODUCTS(?:|/{MatchEverything})$"))
            .And(x => ThenThePriorityIs(1))
            .BDDfy();
    }

    [Fact]
    public void should_create_template_pattern_that_matches_anything_to_end_of_string()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/products/{productId}",
            RouteIsCaseSensitive = true,
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned($"^/api/products(?:|/{MatchEverything})$"))
            .And(x => ThenThePriorityIs(1))
            .BDDfy();
    }

    [Fact]
    public void should_create_template_pattern_that_matches_more_than_one_placeholder()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/products/{productId}/variants/{variantId}",
            RouteIsCaseSensitive = true,
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned($"^/api/products/[^/]+/variants(?:|/{MatchEverything})$"))
            .And(x => ThenThePriorityIs(1))
            .BDDfy();
    }

    [Fact]
    public void should_create_template_pattern_that_matches_more_than_one_placeholder_with_trailing_slash()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/products/{productId}/variants/{variantId}/",
            RouteIsCaseSensitive = true,
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned("^/api/products/[^/]+/variants/[^/]+(/|)$"))
            .And(x => ThenThePriorityIs(1))
            .BDDfy();
    }

    [Fact]
    public void should_create_template_pattern_that_matches_to_end_of_string()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/",
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned("^/$"))
            .And(x => ThenThePriorityIs(1))
            .BDDfy();
    }

    [Fact]
    public void should_create_template_pattern_that_matches_to_end_of_string_when_slash_and_placeholder()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/{url}",
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned("^/.*"))
            .And(x => ThenThePriorityIs(0))
            .BDDfy();
    }

    [Fact]
    public void should_create_template_pattern_that_starts_with_placeholder_then_has_another_later()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/{productId}/products/variants/{variantId}/",
            RouteIsCaseSensitive = true,
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned("^/[^/]+/products/variants/[^/]+(/|)$"))
            .And(x => ThenThePriorityIs(1))
            .BDDfy();
    }

    [Fact]
    public void should_create_template_pattern_that_matches_query_string()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned($@"^(?i)/api/subscriptions/[^/]+/updates(/$|/\?|\?|$)unitId={MatchEverything}$"))
            .And(x => ThenThePriorityIs(1))
            .BDDfy();
    }

    [Fact]
    public void should_create_template_pattern_that_matches_query_string_with_multiple_params()
    {
        var fileRoute = new FileRoute
        {
            UpstreamPathTemplate = "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}&productId={productId}",
        };

        this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
            .When(x => x.WhenICreateTheTemplatePattern())
            .Then(x => x.ThenTheFollowingIsReturned($@"^(?i)/api/subscriptions/[^/]+/updates(/$|/\?|\?|$)unitId={MatchEverything}&productId={MatchEverything}$"))
            .And(x => ThenThePriorityIs(1))
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "2064")]
    [InlineData("/{tenantId}/products?{everything}", "/1/products/1", false)]
    [InlineData("/{tenantId}/products/{everything}", "/1/products/1", true)]
    public void Should_not_match_when_placeholder_appears_after_query_start(string urlPathTemplate, string requestPath, bool shouldMatch)
    {
        // Arrange
        GivenTheFollowingFileRoute(new() { UpstreamPathTemplate = urlPathTemplate });

        // Act
        WhenICreateTheTemplatePattern();

        // Assert
        ShouldMatchWithRegex(requestPath, shouldMatch);
    }

    [Theory]
    [Trait("Bug", "2132")]
    [InlineData("/api/v1/abc?{everything}", "/api/v1/abc2/apple", false)]
    [InlineData("/api/v1/abc2/{everything}", "/api/v1/abc2/apple", true)]
    public void Should_not_match_with_query_param_wildcard(string urlPathTemplate, string requestPath, bool shouldMatch)
    {
        // Arrange
        GivenTheFollowingFileRoute(new() { UpstreamPathTemplate = urlPathTemplate });

        // Act
        WhenICreateTheTemplatePattern();

        // Assert
        ShouldMatchWithRegex(requestPath, shouldMatch);
    }

    private void ShouldMatchWithRegex(string requestPath, bool shouldMatch)
    {
        var match = Regex.Match(requestPath, _result.Template);
        Assert.Equal(shouldMatch, match.Success);
    }

    private void GivenTheFollowingFileRoute(FileRoute fileRoute)
    {
        _fileRoute = fileRoute;
    }

    private void WhenICreateTheTemplatePattern()
    {
        _result = _creator.Create(_fileRoute);
    }

    private void ThenTheFollowingIsReturned(string expected)
    {
        _result.Template.ShouldBe(expected);
    }

    private void ThenThePriorityIs(int v)
    {
        _result.Priority.ShouldBe(v);
    }
}
