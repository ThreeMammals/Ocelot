using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Values;
using System.Text.RegularExpressions;

namespace Ocelot.UnitTests.DownstreamRouteFinder.UrlMatcher;

public class RegExUrlMatcherTests : UnitTest
{
    private readonly RegExUrlMatcher _matcher = new();
    private static readonly string Empty = string.Empty;

    [Fact]
    public void Should_not_match()
    {
        // Arrange
        const string path = "/api/v1/aaaaaaaaa/cards";
        const string downstreamPathTemplate = "^(?i)/api/v[^/]+/cards$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeFalse();
    }

    [Fact]
    public void Should_match()
    {
        // Arrange
        const string path = "/api/v1/cards";
        const string downstreamPathTemplate = "^(?i)/api/v[^/]+/cards$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Should_match_path_with_no_query_string()
    {
        // Arrange
        const string regExForwardSlashAndOnePlaceHolder = "^(?i)/newThing$";
        const string path = "/newThing";
        const string queryString = "?DeviceType=IphoneApp&Browser=moonpigIphone&BrowserString=-&CountryCode=123&DeviceName=iPhone 5 (GSM+CDMA)&OperatingSystem=iPhone OS 7.1.2&BrowserVersion=3708AdHoc&ipAddress=-";
        const string downstreamPathTemplate = regExForwardSlashAndOnePlaceHolder;
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, queryString, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Should_match_query_string()
    {
        // Arrange
        const string regExForwardSlashAndOnePlaceHolder = "^(?i)/api/subscriptions/[^/]+/updates\\?unitId=.+$";
        const string path = "/api/subscriptions/1/updates";
        const string queryString = "?unitId=2";
        const string downstreamPathTemplate = regExForwardSlashAndOnePlaceHolder;
        const bool containsQueryString = true;
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate, containsQueryString);

        // Act
        var result = _matcher.Match(path, queryString, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Should_match_query_string_with_multiple_params()
    {
        // Arrange
        const string regExForwardSlashAndOnePlaceHolder = "^(?i)/api/subscriptions/[^/]+/updates\\?unitId=.+&productId=.+$";
        const string path = "/api/subscriptions/1/updates?unitId=2";
        const string queryString = "?unitId=2&productId=2";
        const string downstreamPathTemplate = regExForwardSlashAndOnePlaceHolder;
        const bool containsQueryString = true;
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate, containsQueryString);

        // Act
        var result = _matcher.Match(path, queryString, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Should_not_match_slash_becaue_we_need_to_match_something_after_it()
    {
        // Arrange
        const string regExForwardSlashAndOnePlaceHolder = "^/[0-9a-zA-Z].+";
        const string path = "/";
        const string downstreamPathTemplate = regExForwardSlashAndOnePlaceHolder;
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeFalse();
    }

    [Fact]
    public void Should_not_match_forward_slash_only_regex()
    {
        // Arrange
        const string path = "/working/";
        const string downstreamPathTemplate = "^/$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeFalse();
    }

    [Fact]
    public void Should_not_match_issue_134()
    {
        // Arrange
        const string path = "/api/vacancy/1/";
        const string downstreamPathTemplate = "^(?i)/vacancy/[^/]+/$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeFalse();
    }

    [Fact]
    public void Should_match_forward_slash_only_regex()
    {
        // Arrange
        const string path = "/";
        const string downstreamPathTemplate = "^/$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Should_find_match_when_template_smaller_than_valid_path()
    {
        // Arrange
        const string path = "/api/products/2354325435624623464235";
        const string downstreamPathTemplate = "^/api/products/.+$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Should_not_find_match()
    {
        // Arrange
        const string path = "/api/values";
        const string downstreamPathTemplate = "^/$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeFalse();
    }

    [Fact]
    public void Can_match_down_stream_url()
    {
        // Arrange
        const string path = "";
        const string downstreamPathTemplate = "^$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Can_match_down_stream_url_with_no_slash()
    {
        // Arrange
        const string path = "api";
        const string downstreamPathTemplate = "^api$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Can_match_down_stream_url_with_one_slash()
    {
        // Arrange
        const string path = "api/";
        const string downstreamPathTemplate = "^api/$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template()
    {
        // Arrange
        const string path = "api/product/products/";
        const string downstreamPathTemplate = "^api/product/products/$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template_with_one_place_holder()
    {
        // Arrange
        const string path = "api/product/products/1";
        const string downstreamPathTemplate = "^api/product/products/.+$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template_with_two_place_holders()
    {
        // Arrange
        const string path = "api/product/products/1/2";
        const string downstreamPathTemplate = "^api/product/products/[^/]+/.+$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template_with_two_place_holders_seperated_by_something()
    {
        // Arrange
        const string path = "api/product/products/1/categories/2";
        const string downstreamPathTemplate = "^api/product/products/[^/]+/categories/.+$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template_with_three_place_holders_seperated_by_something()
    {
        // Arrange
        const string path = "api/product/products/1/categories/2/variant/123";
        const string downstreamPathTemplate = "^api/product/products/[^/]+/categories/[^/]+/variant/.+$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Can_match_down_stream_url_with_downstream_template_with_three_place_holders()
    {
        // Arrange
        const string path = "api/product/products/1/categories/2/variant/";
        const string downstreamPathTemplate = "^api/product/products/[^/]+/categories/[^/]+/variant/$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Should_ignore_case_sensitivity()
    {
        // Arrange
        const string path = "API/product/products/1/categories/2/variant/";
        const string downstreamPathTemplate = "^(?i)api/product/products/[^/]+/categories/[^/]+/variant/$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeTrue();
    }

    [Fact]
    public void Should_respect_case_sensitivity()
    {
        // Arrange
        const string path = "API/product/products/1/categories/2/variant/";
        const string downstreamPathTemplate = "^api/product/products/[^/]+/categories/[^/]+/variant/$";
        var upt = GivenUpstreamPathTemplate(downstreamPathTemplate);

        // Act
        var result = _matcher.Match(path, Empty, upt);

        // Assert
        result.Match.ShouldBeFalse();
    }

    private static UpstreamPathTemplate GivenUpstreamPathTemplate(string downstreamPathTemplate, bool containsQueryString = false)
        => new(downstreamPathTemplate, 0, containsQueryString, downstreamPathTemplate)
        {
            Pattern = new Regex(downstreamPathTemplate),
        };
}
