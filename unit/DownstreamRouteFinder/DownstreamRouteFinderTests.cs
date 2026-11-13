using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.Values;
using _DownstreamRouteFinder_ = Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteFinder;

namespace Ocelot.UnitTests.DownstreamRouteFinder;

public class DownstreamRouteFinderTests : UnitTest
{
    private readonly _DownstreamRouteFinder_ _routeFinder;
    private readonly Mock<IUrlPathToUrlTemplateMatcher> _mockUrlMatcher;
    private readonly Mock<IHeadersToHeaderTemplatesMatcher> _mockHeadersMatcher;
    private readonly Mock<IPlaceholderNameAndValueFinder> _urlPlaceholderFinder;
    private readonly Mock<IHeaderPlaceholderNameAndValueFinder> _headerPlaceholderFinder;
    private string _upstreamUrlPath;
    private List<Route> _routesConfig;
    private InternalConfiguration _config;
    private UrlMatch _match;
    private string _upstreamHttpMethod;
    private string _upstreamHost;
    private IHeaderDictionary _upstreamHeaders;
    private string _upstreamQuery;

    public DownstreamRouteFinderTests()
    {
        _mockUrlMatcher = new Mock<IUrlPathToUrlTemplateMatcher>();
        _mockHeadersMatcher = new Mock<IHeadersToHeaderTemplatesMatcher>();
        _urlPlaceholderFinder = new Mock<IPlaceholderNameAndValueFinder>();
        _headerPlaceholderFinder = new Mock<IHeaderPlaceholderNameAndValueFinder>();
        _routeFinder = new _DownstreamRouteFinder_(_mockUrlMatcher.Object, _urlPlaceholderFinder.Object, _mockHeadersMatcher.Object, _headerPlaceholderFinder.Object);
        _upstreamUrlPath += "/";
        _upstreamHeaders = new HeaderDictionary();
    }

    [Fact]
    public void Should_return_highest_priority_when_first()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        var expectedRoute = GivenRoute(method: "Post", priority: 1);
        _routesConfig =
        [
            expectedRoute,
            GivenRoute(method: "Post", priority: 0),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheFollowingIsReturned(actual, expectedRoute);
    }

    [Fact]
    public void Should_return_highest_priority_when_lowest()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        var expectedRoute = GivenRoute(method: "Post", priority: 1);
        _routesConfig =
        [
            GivenRoute(method: "Post", priority: 0),
            expectedRoute,
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheFollowingIsReturned(actual, expectedRoute);
    }

    [Fact]
    public void Should_return_route()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheFollowingIsReturned(actual, GivenRoute(priority: 1));
        ThenTheUrlMatcherIsCalledCorrectly();
    }

    [Fact]
    public void Should_not_append_slash_to_upstream_url_path()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "matchInUrlMatcher";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheFollowingIsReturned(actual, GivenRoute(priority: 1));

        // Assert: Then The Url Matcher Is Called Correctly
        _mockUrlMatcher.Verify(x => x.Match(_upstreamUrlPath, _upstreamQuery, _routesConfig[0].UpstreamTemplatePattern), Times.Once);
    }

    [Fact]
    public void Should_return_route_if_upstream_path_and_upstream_template_are_the_same()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheFollowingIsReturned(actual, GivenRoute(priority: 1));
    }

    [Fact]
    public void Should_return_correct_route_for_http_verb()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(downstream: "someDownstreamPath", method: "Get", priority: 1),
            GivenRoute(downstream: "someDownstreamPathForAPost", method: "Post", priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheFollowingIsReturned(actual, GivenRoute(
            downstream: "someDownstreamPathForAPost",
            method: "Post",
            priority: 1));
    }

    [Fact]
    public void Should_not_return_route()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "dontMatchPath/";
        _upstreamQuery = string.Empty;
        _routesConfig =
        [
            GivenRoute(downstream: "somPath", upstream: "somePath", priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(false));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        Assert.Null(actual);
        errors.ShouldNotBeEmpty();
        ThenTheUrlMatcherIsCalledCorrectly();
    }

    [Fact]
    public void ObsoleteGet_ThrowsNotSupportedException()
    {
        var exception = Assert.Throws<NotSupportedException>(() =>
#pragma warning disable CS0618
            _routeFinder.Get("/", string.Empty, "GET", null, string.Empty, new HeaderDictionary())
#pragma warning restore CS0618
        );

        Assert.Equal(_DownstreamRouteFinder_.Use_Get_HttpRequest_IEnumerable_method, exception.Message);
    }

    [Fact]
    public void Should_return_correct_route_for_http_verb_setting_multiple_upstream_http_method()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(upstreamMethods: ["Get", "Post"], priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheFollowingIsReturned(actual,
            GivenRoute(upstreamMethods: ["Post"], priority: 1));
    }

    [Fact]
    public void Should_return_correct_route_for_http_verb_setting_all_upstream_http_method()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(upstreamMethods: [], priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheFollowingIsReturned(actual,
            GivenRoute(upstreamMethods: ["Post"], priority: 1));
    }

    [Fact]
    public void Should_not_return_route_for_http_verb_not_setting_in_upstream_http_method()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(upstreamMethods: ["Get", "Patch", "Delete"], priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        Assert.Null(actual);
        errors.ShouldNotBeEmpty();
        ThenTheUrlMatcherIsNotCalled();
    }

    [Fact]
    public void Should_return_route_when_host_matches()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "MATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(host: "MATCH", priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheFollowingIsReturned(actual, GivenRoute(priority: 1));
        ThenTheUrlMatcherIsCalledCorrectly();
    }

    [Fact]
    public void Should_return_route_when_upstreamhost_is_null()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "MATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(host: null, priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheFollowingIsReturned(actual, GivenRoute(priority: 1));
        ThenTheUrlMatcherIsCalledCorrectly();
    }

    [Fact]
    public void Should_not_return_route_when_host_doesnt_match()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "DONTMATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(host: "MATCH", upstreamMethods: ["Get"], priority: 1),
            GivenRoute(host: "MATCH", upstreamMethods: [], priority: 1), // empty list of methods
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        Assert.Null(actual);
        errors.ShouldNotBeEmpty();
        ThenTheUrlMatcherIsNotCalled();
    }

    [Fact]
    public void Should_not_return_route_when_host_doesnt_match_with_empty_upstream_http_method()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "DONTMATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(host: "MATCH", upstreamMethods: [], priority: 1), // empty list of methods
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        Assert.Null(actual);
        errors.ShouldNotBeEmpty();
        ThenTheUrlMatcherIsNotCalled();
    }

    [Fact]
    public void Should_return_route_when_host_does_match_with_empty_upstream_http_method()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "MATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(host: "MATCH", upstreamMethods: [], priority: 1), // empty list of methods
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheUrlMatcherIsCalledCorrectly(1, 0);
    }

    [Fact]
    public void Should_return_route_when_host_matches_but_null_host_on_same_path_first()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "MATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(downstream: "THENULLPATH", priority: 1),
            GivenRoute(host: "MATCH", priority: 1), // empty list of methods
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        ThenTheFollowingIsReturned(actual, GivenRoute(priority: 1));
        ThenTheUrlMatcherIsCalledCorrectly(1, 0);
        ThenTheUrlMatcherIsCalledCorrectly(1, 1);
    }

    [Fact]
    [Trait("PR", "1312")]
    [Trait("Feat", "360")]
    public void Should_return_route_when_upstream_headers_match()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        var upstreamHeaders = new HeaderDictionary()
        {
            ["header1"] = "headerValue1",
            ["header2"] = "headerValue2",
            ["header3"] = "headerValue3",
        };
        var upstreamHeadersConfig = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["header1"] = new UpstreamHeaderTemplate("headerValue1", "headerValue1"),
            ["header2"] = new UpstreamHeaderTemplate("headerValue2", "headerValue2"),
        };
        var urlPlaceholders = new List<PlaceholderNameAndValue> { new("url", "urlValue") };
        var headerPlaceholders = new List<PlaceholderNameAndValue> { new("header", "headerValue") };
        _upstreamUrlPath += "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHeaders = upstreamHeaders;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(urlPlaceholders));
        GivenTheHeaderPlaceholderAndNameFinderReturns(headerPlaceholders);
        _routesConfig =
        [
            GivenRoute(headers: upstreamHeadersConfig, priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        var r = GivenRoute(
            priority: 1,
            placeholders: [.. urlPlaceholders.Union(headerPlaceholders)]
        );
        ThenTheFollowingIsReturned(actual, r);
        ThenTheUrlMatcherIsCalledCorrectly();
    }

    [Fact]
    [Trait("PR", "1312")]
    [Trait("Feat", "360")]
    public void Should_not_return_route_when_upstream_headers_dont_match()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        var upstreamHeadersConfig = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["header1"] = new UpstreamHeaderTemplate("headerValue1", "headerValue1"),
            ["header2"] = new UpstreamHeaderTemplate("headerValue2", "headerValue2"),
        };
        _upstreamUrlPath += "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHeaders = new HeaderDictionary() { { "header1", "headerValue1" } };
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(headers: upstreamHeadersConfig, priority: 1),
            GivenRoute(headers: upstreamHeadersConfig, priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(false);
        _upstreamHttpMethod = "Get";

        // Act
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);

        // Assert
        errors.ShouldNotBeEmpty();
    }

    [Theory]
    [Trait("Feat", "585")]
    [Trait("Feat", "2319")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    [InlineData(false)]
    [InlineData(true)]
    public void Should_filter_static_routes(bool isDynamic)
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath += "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig =
        [
            GivenRoute(priority: 1),
            GivenRoute(isDynamic: isDynamic, priority: 1),
        ];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act, Assert
        var actual = WhenIFindRoute(out IEnumerable<Error> errors);
        actual.IsDynamic.ShouldBeFalse();

        // Act, Assert 2
        _routesConfig.RemoveAll(r => !r.IsDynamic); // remove all static routes
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        actual = WhenIFindRoute(out errors);
        errors.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Bug", "2428")] // https://github.com/ThreeMammals/Ocelot/issues/2428
    [Trait("PR", "2429")] // https://github.com/ThreeMammals/Ocelot/pull/2429
    public void Should_not_modify_placeholders_of_configured_route()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamQuery = string.Empty;
        _upstreamHttpMethod = "Get";
        var route = GivenRoute(downstream: "/api/products/{id}", upstream: "/products/{id}");
        _routesConfig = [route];
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        GivenTheHeaderPlaceholderAndNameFinderReturns([]);
        _urlPlaceholderFinder
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string, string>((path, query, template) => new OkResponse<List<PlaceholderNameAndValue>>([new("{id}", path.Split('/')[^1])]));

        // Act
        _upstreamUrlPath = "/products/1";
        var first = WhenIFindRoute(out IEnumerable<Error> errors);
        _upstreamUrlPath = "/products/2";
        var second = WhenIFindRoute(out errors);

        // Assert
        route.TemplatePlaceholderNameAndValues.ShouldBeNull(); // !!!
        first.ShouldNotBeSameAs(route);
        first.DownstreamRoute.ShouldBeSameAs(route.DownstreamRoute);
        first.TemplatePlaceholderNameAndValues.ShouldHaveSingleItem().Value.ShouldBe("1");
        second.TemplatePlaceholderNameAndValues.ShouldHaveSingleItem().Value.ShouldBe("2");
    }

    private static Route GivenRoute(bool? isDynamic = null, string downstream = null,
        List<string> upstreamMethods = null, string method = null,
        UpstreamPathTemplate upTemplate = null, string upstream = null, int? priority = null,
        string host = null,
        IDictionary<string, UpstreamHeaderTemplate> headers = null,
        List<PlaceholderNameAndValue> placeholders = null)
    {
        var route = GivenDownstreamRoute(downstream, upstreamMethods, method, upTemplate, upstream, priority);
        upstream ??= "someUpstreamPath";
        upTemplate ??= new(upstream, priority ?? 1, false, upstream);
        upstreamMethods ??= [method ?? HttpMethods.Get];
        return new(isDynamic ?? false, route)
        {
            UpstreamHttpMethod = upstreamMethods.Select(m => new HttpMethod(m)).ToHashSet(),
            UpstreamTemplatePattern = upTemplate,
            UpstreamHost = host,
            UpstreamHeaderTemplates = headers,
            TemplatePlaceholderNameAndValues = placeholders,
        };
    }

    private static DownstreamRoute GivenDownstreamRoute(string downstream = null,
        List<string> upstreamMethods = null, string method = null,
        UpstreamPathTemplate upTemplate = null, string upstream = null, int? priority = null)
        => new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate(downstream ?? "someDownstreamPath")
            .WithUpstreamHttpMethod(upstreamMethods ?? [method ?? HttpMethods.Get])
            .WithUpstreamPathTemplate(upTemplate ?? new(upstream ?? "someUpstreamPath", priority ?? 1, false, upstream ?? "someUpstreamPath"))
            .Build();

    private void GivenTheTemplateVariableAndNameFinderReturns(Response<List<PlaceholderNameAndValue>> response)
    {
        _urlPlaceholderFinder
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(response);
    }

    private void GivenTheHeaderPlaceholderAndNameFinderReturns(List<PlaceholderNameAndValue> placeholders)
    {
        _headerPlaceholderFinder
            .Setup(x => x.Find(It.IsAny<IHeaderDictionary>(), It.IsAny<Dictionary<string, UpstreamHeaderTemplate>>()))
            .Returns(placeholders);
    }

    private DefaultHttpContext GivenHttpContext(string upstreamUrlPath = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = upstreamUrlPath ?? _upstreamUrlPath;
        context.Request.QueryString = new(_upstreamQuery);
        context.Request.Method = _upstreamHttpMethod;
        context.Items.SetIInternalConfiguration(_config);
        context.Request.Host = new(_upstreamHost);
        foreach (var (key, value) in _upstreamHeaders)
        {
            context.Request.Headers[key] = value;
        }
        return context;
    }

    private Route WhenIFindRoute(out IEnumerable<Error> errors)
    {
        var context = GivenHttpContext();
        return _routeFinder.Get(context.Request, out errors);
    }


    private void ThenTheUrlMatcherIsCalledCorrectly()
    {
        _mockUrlMatcher
            .Verify(x => x.Match(_upstreamUrlPath, _upstreamQuery, _routesConfig[0].UpstreamTemplatePattern), Times.Once);
    }

    private void ThenTheUrlMatcherIsCalledCorrectly(int times, int index = 0)
    {
        _mockUrlMatcher
            .Verify(x => x.Match(_upstreamUrlPath, _upstreamQuery, _routesConfig[index].UpstreamTemplatePattern), Times.Exactly(times));
    }

    private void ThenTheUrlMatcherIsNotCalled()
    {
        _mockUrlMatcher
            .Verify(x => x.Match(_upstreamUrlPath, _upstreamQuery, _routesConfig[0].UpstreamTemplatePattern), Times.Never);
    }

    private void GivenTheUrlMatcherReturns(UrlMatch match)
    {
        _match = match;
        _mockUrlMatcher
            .Setup(x => x.Match(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpstreamPathTemplate>()))
            .Returns(_match);
    }

    private void GivenTheHeadersMatcherReturns(bool headersMatch)
    {
        _mockHeadersMatcher
            .Setup(x => x.Match(It.IsAny<IHeaderDictionary>(), It.IsAny<Dictionary<string, UpstreamHeaderTemplate>>()))
            .Returns(headersMatch);
    }

    private void GivenTheConfigurationIs(string adminPath, ServiceProviderConfiguration serviceProviderConfig)
    {
        _config = new InternalConfiguration(_routesConfig.ToArray())
        {
            AdministrationPath = adminPath,
            DownstreamHttpVersion = new Version("1.1"),
            DownstreamHttpVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            HttpHandlerOptions = new(),
            LoadBalancerOptions = new(),
            QoSOptions = new(),
            ServiceProviderConfiguration = serviceProviderConfig,
        };
    }

    private void ThenTheFollowingIsReturned(Route actual, Route expected)
    {
        actual.DownstreamRoute[0].DownstreamPathTemplate.Value.ShouldBe(expected.DownstreamRoute[0].DownstreamPathTemplate.Value);
        actual.UpstreamTemplatePattern.Priority.ShouldBe(expected.UpstreamTemplatePattern.Priority);

        for (var i = 0; i < actual.TemplatePlaceholderNameAndValues.Count; i++)
        {
            actual.TemplatePlaceholderNameAndValues[i].Name.ShouldBe(expected.TemplatePlaceholderNameAndValues[i].Name);
            actual.TemplatePlaceholderNameAndValues[i].Value.ShouldBe(expected.TemplatePlaceholderNameAndValues[i].Value);
        }
    }
}
