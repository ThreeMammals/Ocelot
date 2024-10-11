using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests.Routing;

[Trait("PR", "1312")]
[Trait("Feat", "360")]
public sealed class RoutingBasedOnHeadersTests : Steps, IDisposable
{
    private string _downstreamPath;
    private readonly ServiceHandler _serviceHandler;

    public RoutingBasedOnHeadersTests()
    {
        _serviceHandler = new();
    }

    public override void Dispose()
    {
        _serviceHandler.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Should_match_one_header_value()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, headerValue))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    [Fact]
    public void Should_match_one_header_value_when_more_headers()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader("other", "otherValue"))
            .And(x => GivenIAddAHeader(headerName, headerValue))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    [Fact]
    public void Should_match_two_header_values_when_more_headers()
    {
        var port = PortFinder.GetRandomPort();
        var headerName1 = "country_code";
        var headerValue1 = "PL";
        var headerName2 = "region";
        var headerValue2 = "MAZ";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName1] = headerValue1,
            [headerName2] = headerValue2,
        });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName1, headerValue1))
            .And(x => GivenIAddAHeader("other", "otherValue"))
            .And(x => GivenIAddAHeader(headerName2, headerValue2))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    [Fact]
    public void Should_not_match_one_header_value()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var anotherHeaderValue = "UK";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, anotherHeaderValue))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_not_match_one_header_value_when_no_headers()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_not_match_two_header_values_when_one_different()
    {
        var port = PortFinder.GetRandomPort();
        var headerName1 = "country_code";
        var headerValue1 = "PL";
        var headerName2 = "region";
        var headerValue2 = "MAZ";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName1] = headerValue1,
            [headerName2] = headerValue2,
        });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName1, headerValue1))
            .And(x => GivenIAddAHeader("other", "otherValue"))
            .And(x => GivenIAddAHeader(headerName2, "anothervalue"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_not_match_two_header_values_when_one_not_existing()
    {
        var port = PortFinder.GetRandomPort();
        var headerName1 = "country_code";
        var headerValue1 = "PL";
        var headerName2 = "region";
        var headerValue2 = "MAZ";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName1] = headerValue1,
            [headerName2] = headerValue2,
        });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName1, headerValue1))
            .And(x => GivenIAddAHeader("other", "otherValue"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_not_match_one_header_value_when_header_duplicated()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, headerValue))
            .And(x => GivenIAddAHeader(headerName, "othervalue"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_aggregated_route_match_header_value()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var routeA = GivenRoute(port1, "/a", "Laura");
        var routeB = GivenRoute(port2, "/b", "Tom");
        var route = GivenAggRouteWithUpstreamHeaderTemplates(new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(routeA, routeB);
        configuration.Aggregates.Add(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port1, "/a", HttpStatusCode.OK, Hello("Laura")))
            .And(x => GivenThereIsAServiceRunningOn(port2, "/b", HttpStatusCode.OK, Hello("Tom")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, headerValue))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_aggregated_route_not_match_header_value()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var routeA = GivenRoute(port1, "/a", "Laura");
        var routeB = GivenRoute(port2, "/b", "Tom");
        var route = GivenAggRouteWithUpstreamHeaderTemplates(new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(routeA, routeB);
        configuration.Aggregates.Add(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port1, "/a", HttpStatusCode.OK, Hello("Laura")))
            .And(x => x.GivenThereIsAServiceRunningOn(port2, "/b", HttpStatusCode.OK, Hello("Tom")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_match_header_placeholder()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "Region";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, "/products", "/api.internal-{code}/products",
            new()
            {
                [headerName] = "{header:code}",
            });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api.internal-uk/products", HttpStatusCode.OK, Hello("UK")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, "uk"))
            .When(x => WhenIGetUrlOnTheApiGateway("/products"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello("UK")))
            .BDDfy();
    }

    [Fact]
    public void Should_match_header_placeholder_not_in_downstream_path()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "ProductName";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, "/products", "/products-info",
            new()
            {
                [headerName] = "product-{header:everything}",
            });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/products-info", HttpStatusCode.OK, Hello("products")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, "product-Camera"))
            .When(x => WhenIGetUrlOnTheApiGateway("/products"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello("products")))
            .BDDfy();
    }

    [Fact]
    public void Should_distinguish_route_for_different_roles()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "Origin";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, "/products", "/products-admin",
            new()
            {
                [headerName] = "admin.xxx.com",
            });
        var route2 = GivenRouteWithUpstreamHeaderTemplates(port, "/products", "/products", null);
        var configuration = GivenConfiguration(route, route2);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/products-admin", HttpStatusCode.OK, Hello("products admin")))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, "admin.xxx.com"))
            .When(x => WhenIGetUrlOnTheApiGateway("/products"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello("products admin")))
            .BDDfy();
    }

    [Fact]
    public void Should_match_header_and_url_placeholders()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, "/{aa}", "/{country_code}/{version}/{aa}",
            new()
            {
                [headerName] = "start_{header:country_code}_version_{header:version}_end",
            });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/pl/v1/bb", HttpStatusCode.OK, Hello()))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, "start_pl_version_v1_end"))
            .When(x => WhenIGetUrlOnTheApiGateway("/bb"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    [Fact]
    public void Should_match_header_with_braces()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, "/", "/aa",
            new()
            {
                [headerName] = "my_{header}",
            });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/aa", HttpStatusCode.OK, Hello()))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, "my_{header}"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    [Fact]
    public void Should_match_two_headers_with_the_same_name()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue1 = "PL";
        var headerValue2 = "UK";
        var route = GivenRouteWithUpstreamHeaderTemplates(port,
            new()
            {
                [headerName] = headerValue1 + ";{header:whatever}",
            });
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenIAddAHeader(headerName, headerValue1))
            .And(x => GivenIAddAHeader(headerName, headerValue2))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(Hello()))
            .BDDfy();
    }

    private static string Hello() => Hello("Jolanta");
    private static string Hello(string who) => $"Hello from {who}";

    private void GivenThereIsAServiceRunningOn(int port)
        => GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, Hello());

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        basePath ??= "/";
        responseBody ??= Hello();
        var baseUrl = DownstreamUrl(port);
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
        {
            _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

            if (_downstreamPath != basePath)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync($"{nameof(_downstreamPath)} is not equal to {nameof(basePath)}");
            }
            else
            {
                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsync(responseBody);
            }
        });
    }

    private void ThenTheDownstreamUrlPathShouldBe(string expected) => _downstreamPath.ShouldBe(expected);

    private static FileRoute GivenRoute(int port, string path = null, string key = null) => new()
    {
        DownstreamPathTemplate = path ?? "/",
        DownstreamScheme = "http",
        DownstreamHostAndPorts = new()
        {
            new("localhost", port),
        },
        UpstreamPathTemplate = path ?? "/",
        UpstreamHttpMethod = new() { HttpMethods.Get },
        Key = key,
    };

    private static FileRoute GivenRouteWithUpstreamHeaderTemplates(int port, Dictionary<string, string> templates)
    {
        var route = GivenRoute(port);
        route.UpstreamHeaderTemplates = templates;
        return route;
    }

    private static FileRoute GivenRouteWithUpstreamHeaderTemplates(int port, string upstream, string downstream, Dictionary<string, string> templates)
    {
        var route = GivenRoute(port);
        route.UpstreamHeaderTemplates = templates;
        route.UpstreamPathTemplate = upstream ?? "/";
        route.DownstreamPathTemplate = downstream ?? "/";
        return route;
    }

    private static FileAggregateRoute GivenAggRouteWithUpstreamHeaderTemplates(Dictionary<string, string> templates) => new()
    {
        UpstreamPathTemplate = "/",
        UpstreamHost = "localhost",
        RouteKeys = new() { "Laura", "Tom" },
        UpstreamHeaderTemplates = templates,
    };
}
