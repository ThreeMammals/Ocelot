namespace Ocelot.AcceptanceTests;

public sealed class RequestIdTests : Steps
{
    public const string RequestIdKey = "Oc-RequestId";

    [BddfyFact]
    public void Should_use_default_request_id_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.RequestIdKey = RequestIdKey;
        var configuration = GivenConfiguration(route);
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheRequestIdIsReturned())
        .BDDfy();
    }

    [BddfyFact]
    public void Should_use_request_id_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        var requestId = Guid.NewGuid().ToString();
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayWithRequestId("/", requestId))
            .Then(x => ThenTheRequestIdIsReturned(requestId))
        .BDDfy();
    }

    /// <summary>
    /// Now defaults to case insensitive routing but you can override with a setting, also global request id setting available
    /// </summary>
    [BddfyFact]
    [Trait("Commit", "ff57766")] // https://github.com/ThreeMammals/Ocelot/commit/ff5776613f973e838473eedb7f3eeac988da25f0
    public void Should_use_global_request_id_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.RequestIdKey = RequestIdKey;
        var requestId = Guid.NewGuid().ToString();
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayWithRequestId("/", requestId))
            .Then(x => ThenTheRequestIdIsReturned(requestId))
        .BDDfy();
    }

    [BddfyFact]
    [Trait("PR", "329")] // https://github.com/ThreeMammals/Ocelot/pull/329
    public void Should_use_global_request_id_create_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.RequestIdKey = RequestIdKey;
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheRequestIdIsReturned())
        .BDDfy();
    }

    private async Task WhenIGetUrlOnTheApiGatewayWithRequestId(string url, string requestId)
    {
        ocelotClient.DefaultRequestHeaders.TryAddWithoutValidation(RequestIdKey, requestId);
        response = await ocelotClient.GetAsync(url);
    }

    private void GivenThereIsAServiceRunningOn(int port)
    {
        handler.GivenThereIsAServiceRunningOn(port, context =>
        {
            context.Request.Headers.TryGetValue(RequestIdKey, out var requestId);
            context.Response.Headers[RequestIdKey] = requestId;
            return Task.CompletedTask;
        });
    }

    private void ThenTheRequestIdIsReturned()
        => response.Headers.GetValues(RequestIdKey).First().ShouldNotBeNullOrEmpty();
    private void ThenTheRequestIdIsReturned(string expected)
        => response.Headers.GetValues(RequestIdKey).First().ShouldBe(expected);
}
