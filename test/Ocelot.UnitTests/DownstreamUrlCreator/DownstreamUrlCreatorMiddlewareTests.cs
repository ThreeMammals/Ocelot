using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.UnitTests.DownstreamUrlCreator;

public sealed class DownstreamUrlCreatorMiddlewareTests : UnitTest
{
    // TODO: Convert to integration tests to use real IDownstreamPathPlaceholderReplacer service (no mocking). There are a lot of failings
    // private readonly IDownstreamPathPlaceholderReplacer _downstreamUrlTemplateVariableReplacer;
    private readonly Mock<IDownstreamPathPlaceholderReplacer> _downstreamUrlTemplateVariableReplacer;

    private OkResponse<DownstreamPath> _downstreamPath;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private DownstreamUrlCreatorMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly HttpRequestMessage _request;
    private readonly HttpContext _httpContext;
    private readonly Mock<IRequestScopedDataRepository> _repo;

    public DownstreamUrlCreatorMiddlewareTests()
    {
        _repo = new Mock<IRequestScopedDataRepository>();
        _httpContext = new DefaultHttpContext();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<DownstreamUrlCreatorMiddleware>()).Returns(_logger.Object);
        _downstreamUrlTemplateVariableReplacer = new Mock<IDownstreamPathPlaceholderReplacer>();
        _request = new HttpRequestMessage(HttpMethod.Get, "https://my.url/abc/?q=123");
        _next = context => Task.CompletedTask;
    }

    [Fact]
    public async Task Should_replace_scheme_and_path()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate("any old string")
            .WithUpstreamHttpMethod(new List<string> { "Get" })
            .WithDownstreamScheme("https")
            .Build();
        var config = new ServiceProviderConfigurationBuilder()
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build()
        ));
        GivenTheDownstreamRequestUriIs("http://my.url/abc?q=123");
        GivenTheServiceProviderConfigIs(config);
        GivenTheUrlReplacerWillReturn("/api/products/1");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("https://my.url:80/api/products/1?q=123");
        ThenTheQueryStringIs("?q=123");
    }

    [Fact]
    public async Task Should_replace_query_string()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate("/api/units/{subscriptionId}/{unitId}/updates")
            .WithUpstreamHttpMethod(new List<string> { "Get" })
            .WithDownstreamScheme("https")
            .Build();
        var config = new ServiceProviderConfigurationBuilder()
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>
            {
                new("{subscriptionId}", "1"),
                new("{unitId}", "2"),
            },
            new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build()
        ));
        GivenTheDownstreamRequestUriIs("http://localhost:5000/api/subscriptions/1/updates?unitId=2");
        GivenTheServiceProviderConfigIs(config);
        GivenTheUrlReplacerWillReturn("api/units/1/2/updates");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("https://localhost:5000/api/units/1/2/updates");
        ThenTheQueryStringIs(string.Empty);
    }

    [Fact]
    public async Task Should_replace_query_string_but_leave_non_placeholder_queries()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate("/api/units/{subscriptionId}/{unitId}/updates")
            .WithUpstreamHttpMethod(new List<string> { "Get" })
            .WithDownstreamScheme("https")
            .Build();
        var config = new ServiceProviderConfigurationBuilder()
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>
            {
                new("{subscriptionId}", "1"),
                new("{unitId}", "2"),
            },
            new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build()
        ));
        GivenTheDownstreamRequestUriIs("http://localhost:5000/api/subscriptions/1/updates?unitId=2&productId=2"); // unitId is the first
        GivenTheServiceProviderConfigIs(config);
        GivenTheUrlReplacerWillReturn("api/units/1/2/updates");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("https://localhost:5000/api/units/1/2/updates?productId=2");
        ThenTheQueryStringIs("?productId=2");
    }

    [Fact]
    public async Task Should_replace_query_string_but_leave_non_placeholder_queries_2()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate("/api/units/{subscriptionId}/{unitId}/updates")
            .WithUpstreamHttpMethod(new List<string> { "Get" })
            .WithDownstreamScheme("https")
            .Build();
        var config = new ServiceProviderConfigurationBuilder()
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>
            {
                new("{subscriptionId}", "1"),
                new("{unitId}", "2"),
            },
            new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build()
        ));
        GivenTheDownstreamRequestUriIs("http://localhost:5000/api/subscriptions/1/updates?productId=2&unitId=2"); // unitId is the second
        GivenTheServiceProviderConfigIs(config);
        GivenTheUrlReplacerWillReturn("api/units/1/2/updates");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("https://localhost:5000/api/units/1/2/updates?productId=2");
        ThenTheQueryStringIs("?productId=2");
    }

    [Fact]
    public async Task Should_replace_query_string_exact_match()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate("/api/units/{subscriptionId}/{unitId}/updates/{unitIdIty}")
            .WithUpstreamHttpMethod(new List<string> { "Get" })
            .WithDownstreamScheme("https")
            .Build();
        var config = new ServiceProviderConfigurationBuilder()
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>
            {
                new("{subscriptionId}", "1"),
                new("{unitId}", "2"),
                new("{unitIdIty}", "3"),
            },
            new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build()
        ));
        GivenTheDownstreamRequestUriIs("http://localhost:5000/api/subscriptions/1/updates?unitId=2?unitIdIty=3");
        GivenTheServiceProviderConfigIs(config);
        GivenTheUrlReplacerWillReturn("api/units/1/2/updates/3");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("https://localhost:5000/api/units/1/2/updates/3");
        ThenTheQueryStringIs(string.Empty);
    }

    [Fact]
    public async Task Should_not_create_service_fabric_url()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate("any old string")
            .WithUpstreamHttpMethod(new List<string> { "Get" })
            .WithDownstreamScheme("https")
            .Build();
        var config = new ServiceProviderConfigurationBuilder()
            .WithType("ServiceFabric")
            .WithHost("localhost")
            .WithPort(19081)
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build()
        ));
        GivenTheDownstreamRequestUriIs("http://my.url/abc?q=123");
        GivenTheServiceProviderConfigIs(config);
        GivenTheUrlReplacerWillReturn("/api/products/1");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("https://my.url:80/api/products/1?q=123");
    }

    [Fact]
    public async Task Should_create_service_fabric_url()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamScheme("http")
            .WithServiceName("Ocelot/OcelotApp")
            .WithUseServiceDiscovery(true)
            .Build();
        var downstreamRouteHolder = new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            new RouteBuilder().WithDownstreamRoute(downstreamRoute).Build());
        var config = new ServiceProviderConfigurationBuilder()
            .WithType("ServiceFabric")
            .WithHost("localhost")
            .WithPort(19081)
            .Build();
        GivenTheDownStreamRouteIs(downstreamRouteHolder);
        GivenTheServiceProviderConfigIs(config);
        GivenTheDownstreamRequestUriIs("http://localhost:19081");
        GivenTheUrlReplacerWillReturnSequence("/api/products/1", "Ocelot/OcelotApp");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("http://localhost:19081/Ocelot/OcelotApp/api/products/1");
    }

    [Fact]
    public async Task Should_create_service_fabric_url_with_query_string_for_stateless_service()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamScheme("http")
            .WithServiceName("Ocelot/OcelotApp")
            .WithUseServiceDiscovery(true)
            .Build();
        var downstreamRouteHolder = new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            new RouteBuilder().WithDownstreamRoute(downstreamRoute).Build());
        var config = new ServiceProviderConfigurationBuilder()
            .WithType("ServiceFabric")
            .WithHost("localhost")
            .WithPort(19081)
            .Build();
        GivenTheDownStreamRouteIs(downstreamRouteHolder);
        GivenTheServiceProviderConfigIs(config);
        GivenTheDownstreamRequestUriIs("http://localhost:19081?Tom=test&laura=1");
        GivenTheUrlReplacerWillReturnSequence("/api/products/1", "Ocelot/OcelotApp");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("http://localhost:19081/Ocelot/OcelotApp/api/products/1?Tom=test&laura=1");
    }

    [Fact]
    public async Task Should_create_service_fabric_url_with_query_string_for_stateful_service()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamScheme("http")
            .WithServiceName("Ocelot/OcelotApp")
            .WithUseServiceDiscovery(true)
            .Build();
        var downstreamRouteHolder = new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            new RouteBuilder().WithDownstreamRoute(downstreamRoute).Build());
        var config = new ServiceProviderConfigurationBuilder()
            .WithType("ServiceFabric")
            .WithHost("localhost")
            .WithPort(19081)
            .Build();
        GivenTheDownStreamRouteIs(downstreamRouteHolder);
        GivenTheServiceProviderConfigIs(config);
        GivenTheDownstreamRequestUriIs("http://localhost:19081?PartitionKind=test&PartitionKey=1");
        GivenTheUrlReplacerWillReturnSequence("/api/products/1", "Ocelot/OcelotApp");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("http://localhost:19081/Ocelot/OcelotApp/api/products/1?PartitionKind=test&PartitionKey=1");
    }

    [Fact]
    public async Task Should_create_service_fabric_url_with_version_from_upstream_path_template()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithDownstreamScheme("http")
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().WithOriginalValue("/products").Build())
            .WithUseServiceDiscovery(true)
            .Build();
        var routeHolder = new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            new RouteBuilder().WithDownstreamRoute(route).Build());
        var config = new ServiceProviderConfigurationBuilder()
            .WithType("ServiceFabric")
            .WithHost("localhost")
            .WithPort(19081)
            .Build();
        GivenTheDownStreamRouteIs(routeHolder);
        GivenTheServiceProviderConfigIs(config);
        GivenTheDownstreamRequestUriIs("http://localhost:19081?PartitionKind=test&PartitionKey=1");
        GivenTheUrlReplacerWillReturnSequence("/products", "Service_1.0/Api");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("http://localhost:19081/Service_1.0/Api/products?PartitionKind=test&PartitionKey=1");
    }

    [Fact]
    [Trait("Bug", "473")]
    public async Task Should_not_remove_additional_query_parameter_when_placeholder_and_parameter_names_are_different()
    {
        // Arrange
        var methods = new List<string> { "Post", "Get" };
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                .WithOriginalValue("/uc/Authorized/{servak}/{action}").Build())
            .WithDownstreamPathTemplate("/Authorized/{action}?server={servak}")
            .WithUpstreamHttpMethod(methods)
            .WithDownstreamScheme(Uri.UriSchemeHttp)
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>
            {
                new("{action}", "1"),
                new("{servak}", "2"),
            },
            new RouteBuilder().WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(methods)
                .Build()
        ));
        GivenTheDownstreamRequestUriIs("http://localhost:5000/uc/Authorized/2/1/refresh?refreshToken=123456789");
        GivenTheServiceProviderConfigIs(new ServiceProviderConfigurationBuilder().Build());
        GivenTheUrlReplacerWillReturn("/Authorized/1?server=2");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("http://localhost:5000/Authorized/1?server=2&refreshToken=123456789");
        ThenTheQueryStringIs("?server=2&refreshToken=123456789");
    }

    [Fact]
    public async Task Should_not_replace_by_empty_scheme()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamScheme(string.Empty)
            .WithServiceName("Ocelot/OcelotApp")
            .WithUseServiceDiscovery(true)
            .Build();
        var downstreamRouteHolder = new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>(),
            new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .Build());
        var config = new ServiceProviderConfigurationBuilder()
            .WithType("ServiceFabric")
            .WithHost("localhost")
            .WithPort(19081)
            .Build();
        GivenTheDownStreamRouteIs(downstreamRouteHolder);
        GivenTheServiceProviderConfigIs(config);
        GivenTheDownstreamRequestUriIs("https://localhost:19081?PartitionKind=test&PartitionKey=1");
        GivenTheUrlReplacerWillReturnSequence("/api/products/1", "Ocelot/OcelotApp");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("https://localhost:19081/Ocelot/OcelotApp/api/products/1?PartitionKind=test&PartitionKey=1");
    }

    [Fact]
    [Trait("Bug", "952")]
    public async Task Should_map_query_parameters_with_different_names()
    {
        // Arrange
        var methods = new List<string> { "Post", "Get" };
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                .WithOriginalValue("/users?userId={userId}").Build())
            .WithDownstreamPathTemplate("/persons?personId={userId}")
            .WithUpstreamHttpMethod(methods)
            .WithDownstreamScheme(Uri.UriSchemeHttp)
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>
            {
                new("{userId}", "webley"),
            },
            new RouteBuilder().WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(methods)
                .Build()
        ));
        GivenTheDownstreamRequestUriIs($"http://localhost:5000/users?userId=webley");
        GivenTheServiceProviderConfigIs(new ServiceProviderConfigurationBuilder().Build());
        GivenTheUrlReplacerWillReturn("/persons?personId=webley");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs($"http://localhost:5000/persons?personId=webley");
        ThenTheQueryStringIs($"?personId=webley");
    }

    [Fact]
    [Trait("Bug", "952")]
    public async Task Should_map_query_parameters_with_different_names_and_save_old_param_if_placeholder_and_param_names_differ()
    {
        // Arrange
        var methods = new List<string> { "Post", "Get" };
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                .WithOriginalValue("/users?userId={uid}").Build())
            .WithDownstreamPathTemplate("/persons?personId={uid}")
            .WithUpstreamHttpMethod(methods)
            .WithDownstreamScheme(Uri.UriSchemeHttp)
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>
            {
                new("{uid}", "webley"),
            },
            new RouteBuilder().WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(methods)
                .Build()
        ));
        GivenTheDownstreamRequestUriIs($"http://localhost:5000/users?userId=webley");
        GivenTheServiceProviderConfigIs(new ServiceProviderConfigurationBuilder().Build());
        GivenTheUrlReplacerWillReturn("/persons?personId=webley");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs($"http://localhost:5000/persons?personId=webley&userId=webley");
        ThenTheQueryStringIs($"?personId=webley&userId=webley");
    }

    [Theory]
    [Trait("Bug", "1174")]
    [InlineData("projectNumber=45&startDate=2019-12-12&endDate=2019-12-12")]
    [InlineData("$filter=ProjectNumber eq 45 and DateOfSale ge 2020-03-01T00:00:00z and DateOfSale le 2020-03-15T00:00:00z")]
    public async Task Should_forward_query_parameters_without_duplicates(string everythingelse)
    {
        // Arrange
        var methods = new List<string> { "Get" };
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                .WithOriginalValue("/contracts?{everythingelse}").Build())
            .WithDownstreamPathTemplate("/api/contracts?{everythingelse}")
            .WithUpstreamHttpMethod(methods)
            .WithDownstreamScheme(Uri.UriSchemeHttp)
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>
            {
                new("{everythingelse}", everythingelse),
            },
            new RouteBuilder().WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(methods)
                .Build()
        ));
        GivenTheDownstreamRequestUriIs($"http://localhost:5000//contracts?{everythingelse}");
        GivenTheServiceProviderConfigIs(new ServiceProviderConfigurationBuilder().Build());
        GivenTheUrlReplacerWillReturn($"/api/contracts?{everythingelse}");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        var query = everythingelse;
        ThenTheDownstreamRequestUriIs($"http://localhost:5000/api/contracts?{query}");
        ThenTheQueryStringIs($"?{query}");
    }

    [Theory]
    [Trait("Bug", "748")]
    [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1/123", "{url}", "123", "/api/v1/test/123", "")]
    [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1/123?query=1", "{url}", "123", "/api/v1/test/123?query=1", "?query=1")]
    [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1/?query=1", "{url}", "", "/api/v1/test/?query=1", "?query=1")]
    [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1?query=1", "{url}", "", "/api/v1/test?query=1", "?query=1")]
    [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1/", "{url}", "", "/api/v1/test/", "")]
    [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1", "{url}", "", "/api/v1/test", "")]
    public async Task Should_fix_issue_748(string upstreamTemplate, string downstreamTemplate, string requestURL, string placeholderName, string placeholderValue, string downstreamURI, string queryString)
    {
        // Arrange
        var methods = new List<string> { "Get" };
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                .WithOriginalValue(upstreamTemplate).Build())
            .WithDownstreamPathTemplate(downstreamTemplate)
            .WithUpstreamHttpMethod(methods)
            .WithDownstreamScheme(Uri.UriSchemeHttp)
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>
            {
                new(placeholderName, placeholderValue),
                new("{version}", "v1"),
            },
            new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build()
        ));
        GivenTheDownstreamRequestUriIs("http://localhost:5000" + requestURL);
        GivenTheServiceProviderConfigIs(new ServiceProviderConfigurationBuilder().Build());
        GivenTheUrlReplacerWillReturn(downstreamURI);

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs("http://localhost:5000" + downstreamURI);
        ThenTheQueryStringIs(queryString);
    }

    [Fact]
    [Trait("Bug", "2002")]
    public async Task Should_map_when_query_parameters_has_same_names_with_placeholder()
    {
        // Arrange
        const string username = "bbenameur";
        const string groupName = "Paris";
        const string roleid = "123456";
        const string everything = "something=9874565";
        var withGetMethod = new List<string> { "Get" };
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                .WithOriginalValue("/WeatherForecast/{roleid}/groups?username={username}&groupName={groupName}&{everything}")
                .Build())
            .WithDownstreamPathTemplate("/account/{username}/groups/{groupName}/roles?roleId={roleid}&{everything}")
            .WithUpstreamHttpMethod(withGetMethod)
            .WithDownstreamScheme(Uri.UriSchemeHttp)
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>
            {
                new("{username}", username),
                new("{groupName}", groupName),
                new("{roleid}", roleid),
                new("{everything}", everything),
            },
            new RouteBuilder().WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(withGetMethod)
                .Build()
        ));
        GivenTheDownstreamRequestUriIs($"http://localhost:5000/WeatherForecast/{roleid}/groups?username={username}&groupName={groupName}&{everything}");
        GivenTheServiceProviderConfigIs(new ServiceProviderConfigurationBuilder().Build());
        GivenTheUrlReplacerWillReturn($"/account/{username}/groups/{groupName}/roles?roleId={roleid}&{everything}");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs($"http://localhost:5000/account/{username}/groups/{groupName}/roles?roleId={roleid}&{everything}");
        ThenTheQueryStringIs($"?roleId={roleid}&{everything}");
    }

    [Theory]
    [Trait("Bug", "2116")]
    [InlineData("api/debug()")] // no query
    [InlineData("api/debug%28%29")] // debug()
    public async Task ShouldNotFailToHandleUrlWithSpecialRegexChars(string urlPath)
    {
        // Arrange
        var withGetMethod = new List<string> { "Get" };
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                .WithOriginalValue("/routed/api/{path}")
                .Build())
            .WithDownstreamPathTemplate("/api/{path}")
            .WithUpstreamHttpMethod(withGetMethod)
            .WithDownstreamScheme(Uri.UriSchemeHttp)
            .Build();
        GivenTheDownStreamRouteIs(new DownstreamRouteHolder(
            new List<PlaceholderNameAndValue>
            {
                new("{path}", urlPath),
            },
            new RouteBuilder().WithDownstreamRoute(downstreamRoute)
                .WithUpstreamHttpMethod(withGetMethod)
                .Build()
        ));
        GivenTheDownstreamRequestUriIs($"http://localhost:5000/{urlPath}");
        GivenTheServiceProviderConfigIs(new ServiceProviderConfigurationBuilder().Build());
        GivenTheUrlReplacerWillReturn($"routed/{urlPath}");

        // Act
        await WhenICallTheMiddleware();

        // Assert
        ThenTheDownstreamRequestUriIs($"http://localhost:5000/routed/{urlPath}");
        Assert.Equal((int)HttpStatusCode.OK, _httpContext.Response.StatusCode);
    }

    private void GivenTheServiceProviderConfigIs(ServiceProviderConfiguration config)
    {
        var configuration = new InternalConfiguration(null, null, config, null, null, null, null, null, null, null);
        _httpContext.Items.SetIInternalConfiguration(configuration);
    }

    private async Task WhenICallTheMiddleware()
    {
        _middleware = new DownstreamUrlCreatorMiddleware(_next, _loggerFactory.Object, _downstreamUrlTemplateVariableReplacer.Object);
        await _middleware.Invoke(_httpContext);
    }

    private void GivenTheDownStreamRouteIs(DownstreamRouteHolder downstreamRoute)
    {
        _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);
        _httpContext.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
    }

    private void GivenTheDownstreamRequestUriIs(string uri)
    {
        _request.RequestUri = new Uri(uri);
        _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(_request));
    }

    private void GivenTheUrlReplacerWillReturnSequence(params string[] paths)
    {
        var setup = _downstreamUrlTemplateVariableReplacer
            .SetupSequence(x => x.Replace(It.IsAny<string>(), It.IsAny<List<PlaceholderNameAndValue>>()));
        foreach (var path in paths)
        {
            var response = new OkResponse<DownstreamPath>(new DownstreamPath(path));
            setup.Returns(response);
        }
    }

    private void GivenTheUrlReplacerWillReturn(string path)
    {
        _downstreamPath = new OkResponse<DownstreamPath>(new DownstreamPath(path));
        _downstreamUrlTemplateVariableReplacer
            .Setup(x => x.Replace(It.IsAny<string>(), It.IsAny<List<PlaceholderNameAndValue>>()))
            .Returns(_downstreamPath);
    }

    private void ThenTheDownstreamRequestUriIs(string expectedUri)
    {
        _httpContext.Items.DownstreamRequest().ToHttpRequestMessage().RequestUri.OriginalString.ShouldBe(expectedUri);
    }

    private void ThenTheQueryStringIs(string queryString)
    {
        _httpContext.Items.DownstreamRequest().Query.ShouldBe(queryString);
    }
}
