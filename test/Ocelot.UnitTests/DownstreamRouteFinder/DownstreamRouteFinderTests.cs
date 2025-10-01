using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
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
    private Response<DownstreamRouteHolder> _result;
    private List<Route> _routesConfig;
    private InternalConfiguration _config;
    private UrlMatch _match;
    private string _upstreamHttpMethod;
    private string _upstreamHost;
    private Dictionary<string, string> _upstreamHeaders;
    private string _upstreamQuery;

    public DownstreamRouteFinderTests()
    {
        _mockUrlMatcher = new Mock<IUrlPathToUrlTemplateMatcher>();
        _mockHeadersMatcher = new Mock<IHeadersToHeaderTemplatesMatcher>();
        _urlPlaceholderFinder = new Mock<IPlaceholderNameAndValueFinder>();
        _headerPlaceholderFinder = new Mock<IHeaderPlaceholderNameAndValueFinder>();
        _routeFinder = new _DownstreamRouteFinder_(_mockUrlMatcher.Object, _urlPlaceholderFinder.Object, _mockHeadersMatcher.Object, _headerPlaceholderFinder.Object);
    }

    [Fact]
    public void Should_return_highest_priority_when_first()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        var expectedRoute = GivenRoute(method: "Post", priority: 1);
        _routesConfig = new()
        {
            expectedRoute,
            GivenRoute(method: "Post", priority: 0),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            new List<PlaceholderNameAndValue>(),
            expectedRoute));
    }

    [Fact]
    public void Should_return_highest_priority_when_lowest()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        var expectedRoute = GivenRoute(method: "Post", priority: 1);
        _routesConfig = new()
        {
            GivenRoute(method: "Post", priority: 0),
            expectedRoute,
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            new List<PlaceholderNameAndValue>(),
            expectedRoute));
    }

    [Fact]
    public void Should_return_route()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            new List<PlaceholderNameAndValue>(),
            GivenRoute(priority: 1)));
        ThenTheUrlMatcherIsCalledCorrectly();
    }

    [Fact]
    public void Should_not_append_slash_to_upstream_url_path()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "matchInUrlMatcher";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            new List<PlaceholderNameAndValue>(),
            GivenRoute(priority: 1)));

        // Assert: Then The Url Matcher Is Called Correctly
        _mockUrlMatcher.Verify(x => x.Match("matchInUrlMatcher", _upstreamQuery, _routesConfig[0].UpstreamTemplatePattern), Times.Once);
    }

    [Fact]
    public void Should_return_route_if_upstream_path_and_upstream_template_are_the_same()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            new List<PlaceholderNameAndValue>(),
            GivenRoute(priority: 1)));
    }

    [Fact]
    public void Should_return_correct_route_for_http_verb()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(downstream: "someDownstreamPath", method: "Get", priority: 1),
            GivenRoute(downstream: "someDownstreamPathForAPost", method: "Post", priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            new List<PlaceholderNameAndValue>(),
            GivenRoute(downstream: "someDownstreamPathForAPost", method: "Post", priority: 1)
        ));
    }

    [Fact]
    public void Should_not_return_route()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "dontMatchPath/";
        _upstreamQuery = string.Empty;
        _routesConfig = new List<Route>
        {
            GivenRoute(downstream: "somPath", upstream: "somePath", priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(false));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        _result.IsError.ShouldBeTrue();
        ThenTheUrlMatcherIsCalledCorrectly();
    }

    [Fact]
    public void Should_return_correct_route_for_http_verb_setting_multiple_upstream_http_method()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(upstreamMethods: ["Get", "Post"], priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            new List<PlaceholderNameAndValue>(),
            GivenRoute(upstreamMethods: ["Post"], priority: 1)));
    }

    [Fact]
    public void Should_return_correct_route_for_http_verb_setting_all_upstream_http_method()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(upstreamMethods: [], priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            new List<PlaceholderNameAndValue>(),
            GivenRoute(upstreamMethods: ["Post"], priority: 1)));
    }

    [Fact]
    public void Should_not_return_route_for_http_verb_not_setting_in_upstream_http_method()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "someUpstreamPath";
        _upstreamQuery = string.Empty;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(upstreamMethods: ["Get", "Patch", "Delete"], priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Post";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        _result.IsError.ShouldBeTrue();
        ThenTheUrlMatcherIsNotCalled();
    }

    [Fact]
    public void Should_return_route_when_host_matches()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "MATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(host: "MATCH", priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            new List<PlaceholderNameAndValue>(),
            GivenRoute(priority: 1)));
        ThenTheUrlMatcherIsCalledCorrectly();
    }

    [Fact]
    public void Should_return_route_when_upstreamhost_is_null()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "MATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(host: null, priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            new List<PlaceholderNameAndValue>(),
            GivenRoute(priority: 1)));
        ThenTheUrlMatcherIsCalledCorrectly();
    }

    [Fact]
    public void Should_not_return_route_when_host_doesnt_match()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "DONTMATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(host: "MATCH", upstreamMethods: ["Get"], priority: 1),
            GivenRoute(host: "MATCH", upstreamMethods: [], priority: 1), // empty list of methods
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        _result.IsError.ShouldBeTrue();
        ThenTheUrlMatcherIsNotCalled();
    }

    [Fact]
    public void Should_not_return_route_when_host_doesnt_match_with_empty_upstream_http_method()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "DONTMATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(host: "MATCH", upstreamMethods: [], priority: 1), // empty list of methods
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        _result.IsError.ShouldBeTrue();
        ThenTheUrlMatcherIsNotCalled();
    }

    [Fact]
    public void Should_return_route_when_host_does_match_with_empty_upstream_http_method()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "MATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(host: "MATCH", upstreamMethods: [], priority: 1), // empty list of methods
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheUrlMatcherIsCalledCorrectly(1, 0);
    }

    [Fact]
    public void Should_return_route_when_host_matches_but_null_host_on_same_path_first()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
        _upstreamUrlPath = "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHost = "MATCH";
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(downstream: "THENULLPATH", priority: 1),
            GivenRoute(host: "MATCH", priority: 1), // empty list of methods
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            new List<PlaceholderNameAndValue>(),
            GivenRoute(priority: 1)));
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
        var upstreamHeaders = new Dictionary<string, string>()
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
        _upstreamUrlPath = "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHeaders = upstreamHeaders;
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(urlPlaceholders));
        GivenTheHeaderPlaceholderAndNameFinderReturns(headerPlaceholders);
        _routesConfig = new()
        {
            GivenRoute(headers: upstreamHeadersConfig, priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(true);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        ThenTheFollowingIsReturned(new(
            urlPlaceholders.Union(headerPlaceholders).ToList(),
            GivenRoute(priority: 1)));
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
        _upstreamUrlPath = "matchInUrlMatcher/";
        _upstreamQuery = string.Empty;
        _upstreamHeaders = new Dictionary<string, string>() { { "header1", "headerValue1" } };
        GivenTheTemplateVariableAndNameFinderReturns(new OkResponse<List<PlaceholderNameAndValue>>(new List<PlaceholderNameAndValue>()));
        GivenTheHeaderPlaceholderAndNameFinderReturns(new List<PlaceholderNameAndValue>());
        _routesConfig = new()
        {
            GivenRoute(headers: upstreamHeadersConfig, priority: 1),
            GivenRoute(headers: upstreamHeadersConfig, priority: 1),
        };
        GivenTheConfigurationIs(string.Empty, serviceProviderConfig);
        GivenTheUrlMatcherReturns(new UrlMatch(true));
        GivenTheHeadersMatcherReturns(false);
        _upstreamHttpMethod = "Get";

        // Act
        _result = _routeFinder.Get(_upstreamUrlPath, _upstreamQuery, _upstreamHttpMethod, _config, _upstreamHost, _upstreamHeaders);

        // Assert
        _result.IsError.ShouldBeTrue();
    }

    private static Route GivenRoute(string downstream = null,
        List<string> upstreamMethods = null, string method = null,
        UpstreamPathTemplate upTemplate = null, string upstream = null, int? priority = null,
        string host = null,
        IDictionary<string, UpstreamHeaderTemplate> headers = null)
    {
        var route = GivenDownstreamRoute(downstream, upstreamMethods, method, upTemplate, upstream, priority);
        upstream ??= "someUpstreamPath";
        upTemplate ??= new(upstream, priority ?? 1, false, upstream);
        upstreamMethods ??= [method ?? HttpMethods.Get];
        return new()
        {
            DownstreamRoute = [route],
            UpstreamHttpMethod = upstreamMethods.Select(m => new HttpMethod(m)).ToHashSet(),
            UpstreamTemplatePattern = upTemplate,
            UpstreamHost = host,
            UpstreamHeaderTemplates = headers,
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
            .Setup(x => x.Find(It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, UpstreamHeaderTemplate>>()))
            .Returns(placeholders);
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
            .Setup(x => x.Match(It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, UpstreamHeaderTemplate>>()))
            .Returns(headersMatch);
    }

    private void GivenTheConfigurationIs(string adminPath, ServiceProviderConfiguration serviceProviderConfig)
    {
        _config = new InternalConfiguration(
            _routesConfig,
            adminPath,
            serviceProviderConfig,
            string.Empty,
            new LoadBalancerOptions(),
            string.Empty,
            new QoSOptionsBuilder().Build(),
            new HttpHandlerOptionsBuilder().Build(),
            new Version("1.1"),
            HttpVersionPolicy.RequestVersionOrLower);
    }

    private void ThenTheFollowingIsReturned(DownstreamRouteHolder expected)
    {
        _result.Data.Route.DownstreamRoute[0].DownstreamPathTemplate.Value.ShouldBe(expected.Route.DownstreamRoute[0].DownstreamPathTemplate.Value);
        _result.Data.Route.UpstreamTemplatePattern.Priority.ShouldBe(expected.Route.UpstreamTemplatePattern.Priority);

        for (var i = 0; i < _result.Data.TemplatePlaceholderNameAndValues.Count; i++)
        {
            _result.Data.TemplatePlaceholderNameAndValues[i].Name.ShouldBe(expected.TemplatePlaceholderNameAndValues[i].Name);
            _result.Data.TemplatePlaceholderNameAndValues[i].Value.ShouldBe(expected.TemplatePlaceholderNameAndValues[i].Value);
        }

        _result.IsError.ShouldBeFalse();
    }
}
