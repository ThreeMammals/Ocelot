namespace Ocelot.Acceptance.Routing;

public sealed class CaseSensitiveRoutingTests : Steps
{
    [Fact]
    public async Task Should_return_response_200_when_global_ignore_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/products/{productId}", "/api/products/{productId}");
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/PRODUCTS/1");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_return_response_200_when_route_ignore_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/products/{productId}", "/api/products/{productId}");
        route.RouteIsCaseSensitive = false;
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/PRODUCTS/1");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_return_response_404_when_route_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/products/{productId}", "/api/products/{productId}");
        route.RouteIsCaseSensitive = true;
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/PRODUCTS/1");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_return_response_200_when_route_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/PRODUCTS/{productId}", "/api/products/{productId}");
        route.RouteIsCaseSensitive = true;
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/PRODUCTS/1");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_return_response_404_when_global_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/products/{productId}", "/api/products/{productId}");
        route.RouteIsCaseSensitive = true;
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/PRODUCTS/1");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_return_response_200_when_global_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/PRODUCTS/{productId}", "/api/products/{productId}");
        route.RouteIsCaseSensitive = true;
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/PRODUCTS/1");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }
}
