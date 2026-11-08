namespace Ocelot.Acceptance.Logging;

public sealed class RequestIdTests : Steps
{
    public const string RequestIdKey = "Oc-RequestId";

    [Fact]
    public async Task Should_use_default_request_id_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        route.RequestIdKey = RequestIdKey;
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheRequestIdIsReturned();
    }

    [Fact]
    public async Task Should_use_request_id_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        var requestId = Guid.NewGuid().ToString();
        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGatewayWithRequestId("/", requestId);
        ThenTheRequestIdIsReturned(requestId);
    }

    /// <summary>
    /// Now defaults to case insensitive routing but you can override with a setting, also global request id setting available
    /// </summary>
    [Fact]
    [Trait("Commit", "ff57766")] // https://github.com/ThreeMammals/Ocelot/commit/ff5776613f973e838473eedb7f3eeac988da25f0
    public async Task Should_use_global_request_id_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.RequestIdKey = RequestIdKey;
        var requestId = Guid.NewGuid().ToString();
        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGatewayWithRequestId("/", requestId);
        ThenTheRequestIdIsReturned(requestId);
    }

    [Fact]
    [Trait("PR", "329")] // https://github.com/ThreeMammals/Ocelot/pull/329
    public async Task Should_use_global_request_id_create_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.RequestIdKey = RequestIdKey;
        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheRequestIdIsReturned();
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
