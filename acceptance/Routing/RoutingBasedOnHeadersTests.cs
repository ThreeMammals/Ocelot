using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration.File;

namespace Ocelot.Acceptance.Routing;

[Trait("Feat", "360")] // https://github.com/ThreeMammals/Ocelot/issues/360
public sealed class RoutingBasedOnHeadersTests : Steps
{
    private string _downstreamPath;

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_match_one_header_value()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName, headerValue);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(Hello());
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_match_one_header_value_when_more_headers()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader("other", "otherValue");
        GivenIAddAHeader(headerName, headerValue);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(Hello());
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_match_two_header_values_when_more_headers()
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

        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName1, headerValue1);
        GivenIAddAHeader("other", "otherValue");
        GivenIAddAHeader(headerName2, headerValue2);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(Hello());
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_not_match_one_header_value()
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

        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName, anotherHeaderValue);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_not_match_one_header_value_when_no_headers()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_not_match_two_header_values_when_one_different()
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

        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName1, headerValue1);
        GivenIAddAHeader("other", "otherValue");
        GivenIAddAHeader(headerName2, "anothervalue");
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_not_match_two_header_values_when_one_not_existing()
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

        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName1, headerValue1);
        GivenIAddAHeader("other", "otherValue");
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_not_match_one_header_value_when_header_duplicated()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName, headerValue);
        GivenIAddAHeader(headerName, "othervalue");
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_aggregated_route_match_header_value()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var routeA = GivenRouteWithKey(port1, "/a", "Laura");
        var routeB = GivenRouteWithKey(port2, "/b", "Tom");
        var route = GivenAggRouteWithUpstreamHeaderTemplates(new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(routeA, routeB);
        configuration.Aggregates.Add(route);

        GivenThereIsAServiceRunningOn(port1, "/a", HttpStatusCode.OK, Hello("Laura"));
        GivenThereIsAServiceRunningOn(port2, "/b", HttpStatusCode.OK, Hello("Tom"));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName, headerValue);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_aggregated_route_not_match_header_value()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var routeA = GivenRouteWithKey(port1, "/a", "Laura");
        var routeB = GivenRouteWithKey(port2, "/b", "Tom");
        var route = GivenAggRouteWithUpstreamHeaderTemplates(new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(routeA, routeB);
        configuration.Aggregates.Add(route);

        GivenThereIsAServiceRunningOn(port1, "/a", HttpStatusCode.OK, Hello("Laura"));
        GivenThereIsAServiceRunningOn(port2, "/b", HttpStatusCode.OK, Hello("Tom"));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_match_header_placeholder()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "Region";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, "/products", "/api.internal-{code}/products",
            new()
            {
                [headerName] = "{header:code}",
            });
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port, "/api.internal-uk/products", HttpStatusCode.OK, Hello("UK"));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName, "uk");
        await WhenIGetUrlOnTheApiGateway("/products");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(Hello("UK"));
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_match_header_placeholder_not_in_downstream_path()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "ProductName";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, "/products", "/products-info",
            new()
            {
                [headerName] = "product-{header:everything}",
            });
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port, "/products-info", HttpStatusCode.OK, Hello("products"));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName, "product-Camera");
        await WhenIGetUrlOnTheApiGateway("/products");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(Hello("products"));
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_distinguish_route_for_different_roles()
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

        GivenThereIsAServiceRunningOn(port, "/products-admin", HttpStatusCode.OK, Hello("products admin"));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName, "admin.xxx.com");
        await WhenIGetUrlOnTheApiGateway("/products");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(Hello("products admin"));
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_match_header_and_url_placeholders()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, "/{aa}", "/{country_code}/{version}/{aa}",
            new()
            {
                [headerName] = "start_{header:country_code}_version_{header:version}_end",
            });
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port, "/pl/v1/bb", HttpStatusCode.OK, Hello());
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName, "start_pl_version_v1_end");
        await WhenIGetUrlOnTheApiGateway("/bb");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(Hello());
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_match_header_with_braces()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, "/", "/aa",
            new()
            {
                [headerName] = "my_{header}",
            });
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port, "/aa", HttpStatusCode.OK, Hello());
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName, "my_{header}");
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(Hello());
    }

    [Fact]
    [Trait("PR", "1312")] // https://github.com/ThreeMammals/Ocelot/pull/1312
    public async Task Should_match_two_headers_with_the_same_name()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue1 = "PL";
        var headerValue2 = "UK";
        var multipleValues = new StringValues([headerValue1, "{header:whatever}"]);
        var route = GivenRouteWithUpstreamHeaderTemplates(port,
            new()
            {
                [headerName] = multipleValues.ToString(), // headerValue1 + ";{header:whatever}",
            });
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader(headerName, headerValue1);
        GivenIAddAHeader(headerName, headerValue2);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(Hello());
    }

    [Fact]
    [Trait("Bug", "2352")] // https://github.com/ThreeMammals/Ocelot/issues/2352
    [Trait("PR", "2427")] // https://github.com/ThreeMammals/Ocelot/pull/2427
    public async Task Should_match_one_header_value_when_browser_sends_client_hints()
    {
        var port = PortFinder.GetRandomPort();
        var headerName = "country_code";
        var headerValue = "PL";
        var secChUa = "\"Microsoft Edge\";v=\"143\", \"Chromium\";v=\"143\", \"Not A(Brand\";v=\"24\"";
        var secChUaMobile = "?0";
        var route = GivenRouteWithUpstreamHeaderTemplates(port, new()
        {
            [headerName] = headerValue,
        });
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenIAddAHeader("sec-ch-ua", secChUa);
        GivenIAddAHeader("sec-ch-ua-mobile", secChUaMobile);
        GivenIAddAHeader(headerName, headerValue);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(Hello());
    }

    private static string Hello() => Hello("Jolanta");
    private static string Hello(string who) => $"Hello from {who}";

    private void GivenThereIsAServiceRunningOn(int port)
        => GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, Hello());

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        basePath ??= "/";
        responseBody ??= Hello();
        handler.GivenThereIsAServiceRunningOn(port, basePath, async context =>
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

    private FileRoute GivenRouteWithKey(int port, string path = null, string key = null)
        => GivenRoute(port, path, path).WithKey(key);

    private FileRoute GivenRouteWithUpstreamHeaderTemplates(int port, Dictionary<string, string> templates)
    {
        var route = GivenDefaultRoute(port);
        route.UpstreamHeaderTemplates = templates;
        return route;
    }

    private FileRoute GivenRouteWithUpstreamHeaderTemplates(int port, string upstream, string downstream, Dictionary<string, string> templates)
    {
        var route = GivenRoute(port, upstream, downstream);
        route.UpstreamHeaderTemplates = templates;
        return route;
    }

    private static FileAggregateRoute GivenAggRouteWithUpstreamHeaderTemplates(Dictionary<string, string> templates) => new()
    {
        UpstreamPathTemplate = "/",
        UpstreamHost = "localhost",
        RouteKeys = ["Laura", "Tom"],
        UpstreamHeaderTemplates = templates,
    };
}
