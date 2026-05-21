namespace Ocelot.AcceptanceTests;

public sealed class CaseSensitiveRoutingTests : Steps
{
    [BddfyFact]
    public void Should_return_response_200_when_global_ignore_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/products/{productId}", "/api/products/{productId}");
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
        .BDDfy();
    }

    [BddfyFact]
    public void Should_return_response_200_when_route_ignore_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/products/{productId}", "/api/products/{productId}");
        route.RouteIsCaseSensitive = false;
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
        .BDDfy();
    }

    [BddfyFact]
    public void Should_return_response_404_when_route_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/products/{productId}", "/api/products/{productId}");
        route.RouteIsCaseSensitive = true;
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
        .BDDfy();
    }

    [BddfyFact]
    public void Should_return_response_200_when_route_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/PRODUCTS/{productId}", "/api/products/{productId}");
        route.RouteIsCaseSensitive = true;
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
        .BDDfy();
    }

    [BddfyFact]
    public void Should_return_response_404_when_global_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/products/{productId}", "/api/products/{productId}");
        route.RouteIsCaseSensitive = true;
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
        .BDDfy();
    }

    [BddfyFact]
    public void Should_return_response_200_when_global_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/PRODUCTS/{productId}", "/api/products/{productId}");
        route.RouteIsCaseSensitive = true;
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsAServiceRunningOnPath(port, "/api/products/1", "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
        .BDDfy();
    }
}
