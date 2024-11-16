using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;
using System.Web;

namespace Ocelot.AcceptanceTests.Routing;

public sealed class RoutingTests : Steps, IDisposable
{
    private readonly ServiceHandler _serviceHandler;
    private string _downstreamPath;
    private string _downstreamQuery;

    public RoutingTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    public override void Dispose()
    {
        _serviceHandler.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Should_not_match_forward_slash_in_pattern_before_next_forward_slash()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/api/v{apiVersion}/cards", "/api/v{apiVersion}/cards")
            .SetPriority(1);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/v1/aaaaaaaaa/cards", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/api/v1/aaaaaaaaa/cards"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_404_when_no_configuration_at_all()
    {
        this.Given(x => GivenThereIsAConfiguration(new()))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_forward_slash_and_placeholder_only()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/{url}", "/{url}");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_favouring_forward_slash_with_path_route()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port1, "/{url}", "/{url}");
        var route2 = GivenRoute(port2, "/", "/");
        var configuration = GivenConfiguration(route1, route2);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port1, "/test", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/test"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_favouring_forward_slash()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port1, "/{url}", "/{url}");
        var route2 = GivenRoute(port2, "/", "/");
        var configuration = GivenConfiguration(route1, route2);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port2, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_favouring_forward_slash_route_because_it_is_first()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port1, "/", "/");
        var route2 = GivenRoute(port2, "/{url}", "/{url}");
        var configuration = GivenConfiguration(route1, route2);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port1, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_nothing_and_placeholder_only()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/{url}", "/{url}");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway(string.Empty))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_simple_url()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/", "/");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "134")]
    public void Should_fix_issue_134()
    {
        var port = PortFinder.GetRandomPort(); //var port2 = PortFinder.GetRandomPort();
        var methods = new string[] { HttpMethods.Options, HttpMethods.Put, HttpMethods.Get, HttpMethods.Post, HttpMethods.Delete };
        var route1 = GivenRoute(port, "/vacancy/", "/api/v1/vacancy")
            .WithMethods(methods);
        var route2 = GivenRoute(port, "/vacancy/{vacancyId}", "/api/v1/vacancy/{vacancyId}")
            .WithMethods(methods);
        route1.LoadBalancerOptions.Type = route2.LoadBalancerOptions.Type = nameof(LeastConnection);
        var configuration = GivenConfiguration(route1, route2);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/v1/vacancy/1", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/vacancy/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_when_path_missing_forward_slash_as_first_char()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/", "/api/products");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/products", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_when_host_has_trailing_slash()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/", "/api/products");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/products", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Theory]
    [InlineData("/products")]
    [InlineData("/products/")]
    public void Should_return_ok_when_upstream_url_ends_with_forward_slash_but_template_does_not(string url)
    {
        const string downstreamBasePath = "/products";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, $"{downstreamBasePath}/", downstreamBasePath);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, downstreamBasePath, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway(url))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "649")]
    [InlineData("/account/authenticate")]
    [InlineData("/account/authenticate/")]
    public void Should_fix_issue_649(string url)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/account/authenticate/", "/authenticate");
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.BaseUrl = DownstreamUrl(port);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/authenticate", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway(url))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_not_found_when_upstream_url_ends_with_forward_slash_but_template_does_not()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/products", "/products");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/products", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/products/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "683")]
    [InlineData("/products/{productId}", "/products/{productId}", "/products/")]
    public void Should_return_response_200_with_empty_placeholder(string downstreamPathTemplate, string upstreamPathTemplate, string requestURL)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, upstreamPathTemplate, downstreamPathTemplate);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, requestURL, HttpStatusCode.OK, "Hello from Aly"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway(requestURL))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Aly"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_complex_url()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/products/{productId}", "/api/products/{productId}");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/products/1", HttpStatusCode.OK, "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/products/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Some Product"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_with_complex_url_that_starts_with_placeholder()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/{variantId}/products/{productId}", "/api/{variantId}/products/{productId}");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/23/products/1", HttpStatusCode.OK, "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("23/products/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Some Product"))
            .BDDfy();
    }

    [Fact]
    public void Should_not_add_trailing_slash_to_downstream_url()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/products/{productId}", "/api/products/{productId}");
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsAServiceRunningOn(port, "/api/products/1", HttpStatusCode.OK, "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/products/1"))
            .Then(x => ThenTheDownstreamUrlPathShouldBe("/api/products/1"))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_201_with_simple_url()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/", "/").WithMethods(HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.Created, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenThePostHasContent("postContent"))
            .When(x => WhenIPostUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_201_with_complex_query_string()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/newThing", "/newThing");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/newThing", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/newThing?DeviceType=IphoneApp&Browser=moonpigIphone&BrowserString=-&CountryCode=123&DeviceName=iPhone 5 (GSM+CDMA)&OperatingSystem=iPhone OS 7.1.2&BrowserVersion=3708AdHoc&ipAddress=-"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Theory]
    [Trait("Feat", "89")]
    [InlineData("/api/{finalUrlPath}", "/api/api1/{finalUrlPath}", "/api/api1/product/products/categories/", "/api/product/products/categories/")]
    [InlineData("/api/{urlPath}", "/myApp1Name/api/{urlPath}", "/myApp1Name/api/products/1", "/api/products/1")]
    public void Should_return_response_200_with_placeholder_for_final_url_path2(string downstreamPathTemplate, string upstreamPathTemplate, string requestURL, string downstreamPath)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, upstreamPathTemplate, downstreamPathTemplate);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, downstreamPath, HttpStatusCode.OK, "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway(requestURL))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Some Product"))
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "748")]
    [InlineData("/downstream/test/{everything}", "/upstream/test/{everything}", "/upstream/test/1", "/downstream/test/1", "?p1=v1&p2=v2&something-else")]
    [InlineData("/downstream/test/{everything}", "/upstream/test/{everything}", "/upstream/test/", "/downstream/test/", "?p1=v1&p2=v2&something-else")]
    [InlineData("/downstream/test/{everything}", "/upstream/test/{everything}", "/upstream/test", "/downstream/test", "?p1=v1&p2=v2&something-else")]
    [InlineData("/downstream/test/{everything}", "/upstream/test/{everything}", "/upstream/test123", null, null)]
    [InlineData("/downstream/{version}/test/{everything}", "/upstream/{version}/test/{everything}", "/upstream/v1/test/123", "/downstream/v1/test/123", "?p1=v1&p2=v2&something-else")]
    [InlineData("/downstream/{version}/test", "/upstream/{version}/test", "/upstream/v1/test", "/downstream/v1/test", "?p1=v1&p2=v2&something-else")]
    [InlineData("/downstream/{version}/test", "/upstream/{version}/test", "/upstream/test", null, null)]
    public void Should_return_correct_downstream_when_omitting_ending_placeholder(string downstreamPathTemplate, string upstreamPathTemplate, string requestURL, string downstreamURL, string queryString)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, upstreamPathTemplate, downstreamPathTemplate);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Aly"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway(requestURL))
            .Then(x => ThenTheDownstreamUrlPathShouldBe(downstreamURL))

            // Now check the same URL but with query string
            // Catch-All placeholder should forward any path + query string combinations to the downstream service
            // More: https://ocelot.readthedocs.io/en/latest/features/routing.html#placeholders:~:text=This%20will%20forward%20any%20path%20%2B%20query%20string%20combinations%20to%20the%20downstream%20service%20after%20the%20path%20%2Fapi.
            .When(x => WhenIGetUrlOnTheApiGateway(requestURL + queryString))
            .Then(x => ThenTheDownstreamUrlPathShouldBe(downstreamURL))
            .And(x => ThenTheDownstreamUrlQueryStringShouldBe(queryString))
            .BDDfy();
    }

    [Trait("PR", "1911")]
    [Trait("Link", "https://ocelot.readthedocs.io/en/latest/features/routing.html#catch-all-query-string")]
    [Theory(DisplayName = "Catch All Query String should be forwarded with all query string parameters with(out) last slash")]
    [InlineData("/apipath/contracts?{everything}", "/contracts?{everything}", "/contracts", "/apipath/contracts", "")]
    [InlineData("/apipath/contracts?{everything}", "/contracts?{everything}", "/contracts?", "/apipath/contracts", "")]
    [InlineData("/apipath/contracts?{everything}", "/contracts?{everything}", "/contracts?p1=v1&p2=v2", "/apipath/contracts", "?p1=v1&p2=v2")]
    [InlineData("/apipath/contracts/?{everything}", "/contracts/?{everything}", "/contracts/?", "/apipath/contracts/", "")]
    [InlineData("/apipath/contracts/?{everything}", "/contracts/?{everything}", "/contracts/?p3=v3&p4=v4", "/apipath/contracts/", "?p3=v3&p4=v4")]
    [InlineData("/apipath/contracts?{everything}", "/contracts?{everything}", "/contracts?filter=(-something+123+else)", "/apipath/contracts", "?filter=(-something%20123%20else)")]
    public void Should_forward_Catch_All_query_string_when_last_slash(string downstream, string upstream, string requestURL, string downstreamPath, string queryString)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, upstream, downstream);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsAServiceRunningOn(port, downstreamPath, HttpStatusCode.OK, "Hello from Raman"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway(requestURL))
            .Then(x => ThenTheDownstreamUrlPathShouldBe(downstreamPath)) // !
            .And(x => ThenTheDownstreamUrlQueryStringShouldBe(queryString)) // !!
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Raman"))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "91, 94")]
    public void Should_return_response_201_with_simple_url_and_multiple_upstream_http_method()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/", "/").WithMethods(HttpMethods.Get, HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.Created, nameof(HttpStatusCode.Created)))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenThePostHasContent("postContent"))
            .When(x => WhenIPostUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
            .And(x => ThenTheResponseBodyShouldBe(nameof(HttpStatusCode.Created)))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "91, 94")]
    public void Should_return_response_200_with_simple_url_and_any_upstream_http_method()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/", "/").WithMethods();
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "134")]
    public void Should_return_404_when_calling_upstream_route_with_no_matching_downstream_route()
    {
        var port = PortFinder.GetRandomPort();
        var methods = new string[] { HttpMethods.Options, HttpMethods.Put, HttpMethods.Get, HttpMethods.Post, HttpMethods.Delete };
        var route1 = GivenRoute(port, "/vacancy/", "/api/v1/vacancy").WithMethods(methods);
        var route2 = GivenRoute(port, "/vacancy/{vacancyId}", "/api/v1/vacancy/{vacancyId}").WithMethods(methods);
        route1.LoadBalancerOptions.Type = route2.LoadBalancerOptions.Type = nameof(LeastConnection);
        var configuration = GivenConfiguration(route1, route2);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/v1/vacancy/1", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("api/vacancy/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "145")]
    public void Should_not_set_trailing_slash_on_url_template()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/platform/{url}", "/api/{url}");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/swagger/lib/backbone-min.js", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/platform/swagger/lib/backbone-min.js"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => ThenTheDownstreamUrlPathShouldBe("/api/swagger/lib/backbone-min.js"))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "270, 272")]
    public void Should_use_priority()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port1, "/goods/{url}", "/goods/{url}")
            .SetPriority(0);
        var route2 = GivenRoute(port2, "/goods/delete", "/goods/delete");
        var configuration = GivenConfiguration(route1, route2);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port2, "/goods/delete", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/goods/delete"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "548")]
    public void Should_match_multiple_paths_with_catch_all()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/{everything}", "/{everything}");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/test/toot", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/test/toot"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "271")]
    public void Should_fix_issue_271()
    {
        var port = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(port, "/api/v1/{everything}", "/api/v1/{everything}")
            .WithMethods(HttpMethods.Get, HttpMethods.Put, HttpMethods.Post);
        var route2 = GivenRoute(port2, "/connect/token", "/connect/token")
            .WithMethods(HttpMethods.Post);
        var configuration = GivenConfiguration(route1, route2);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/v1/modules/Test", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/api/v1/modules/Test"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "2116")]
    [InlineData("debug()")] // no query
    [InlineData("debug%28%29")] // debug()
    public void Should_change_downstream_path_by_upstream_path_when_path_contains_malicious_characters(string path)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/api/{path}", "/routed/api/{path}");
        var configuration = GivenConfiguration(route);
        var decodedDownstreamUrlPath = $"/routed/api/{HttpUtility.UrlDecode(path)}";
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, decodedDownstreamUrlPath, HttpStatusCode.OK, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway($"/api/{path}")) // should be encoded
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheDownstreamUrlPathShouldBe(decodedDownstreamUrlPath))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        var baseUrl = DownstreamUrl(port);
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, HttpHandler);

        async Task HttpHandler(HttpContext context)
        {
            _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value)
                ? context.Request.PathBase.Value + context.Request.Path.Value
                : context.Request.Path.Value;
            _downstreamQuery = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;

            if (_downstreamPath != basePath)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync("Downstream path didn't match base path");
            }
            else
            {
                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsync(responseBody);
            }
        }
    }

    private void ThenTheDownstreamUrlPathShouldBe(string expectedDownstreamPath)
    {
        _downstreamPath.ShouldBe(expectedDownstreamPath);
    }

    private void ThenTheDownstreamUrlQueryStringShouldBe(string expectedQueryString)
    {
        _downstreamQuery.ShouldBe(expectedQueryString);
    }

    private static FileRoute GivenRoute(int port, string upstream, string downstream) => new()
    {
        DownstreamPathTemplate = downstream,
        DownstreamScheme = Uri.UriSchemeHttp,
        DownstreamHostAndPorts = { Localhost(port) },
        UpstreamPathTemplate = upstream,
        UpstreamHttpMethod = new() { HttpMethods.Get },
    };
}
