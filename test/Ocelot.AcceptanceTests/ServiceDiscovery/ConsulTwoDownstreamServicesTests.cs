using Consul;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

public sealed class ConsulTwoDownstreamServicesTests : Steps
{
    private readonly List<ServiceEntry> _serviceEntries;

    public ConsulTwoDownstreamServicesTests()
    {
        _serviceEntries = new List<ServiceEntry>();
    }

    [Fact]
    [Trait("Bug", "194")] // https://github.com/ThreeMammals/Ocelot/issues/194
    public void Should_fix_issue_194()
    {
        var consulPort = PortFinder.GetRandomPort();
        var servicePort1 = PortFinder.GetRandomPort();
        var servicePort2 = PortFinder.GetRandomPort();
        var route1 = GivenRoute(servicePort1, "/api/user/{user}", "/api/user/{user}");
        var route2 = GivenRoute(servicePort2, "/api/product/{product}", "/api/product/{product}");
        var configuration = GivenConfiguration(route1, route2);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = Uri.UriSchemeHttps,
            Host = "localhost",
            Port = consulPort,
        };
        this.Given(x => x.GivenProductServiceIsRunning(servicePort1, "/api/user/info", HttpStatusCode.OK, "user"))
            .And(x => x.GivenProductServiceIsRunning(servicePort2, "/api/product/info", HttpStatusCode.OK, "product"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(consulPort))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/api/user/info?id=1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("user"))
            .When(x => WhenIGetUrlOnTheApiGateway("/api/product/info?id=1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("product"))
            .BDDfy();
    }

    private void GivenThereIsAFakeConsulServiceDiscoveryProvider(int port)
    {
        handler.GivenThereIsAServiceRunningOn(port, context =>
        {
            if (context.Request.Path.Value == "/v1/health/service/product")
            {
                var json = JsonConvert.SerializeObject(_serviceEntries);
                context.Response.Headers.Append("Content-Type", "application/json");
                return context.Response.WriteAsync(json);
            }
            return context.Response.WriteAsync(string.Empty);
        });
    }

    private void GivenProductServiceIsRunning(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, context =>
        {
            var downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;
            bool oK = downstreamPath == basePath;
            context.Response.StatusCode = oK ? (int)statusCode : (int)HttpStatusCode.NotFound;
            return context.Response.WriteAsync(oK ? responseBody : "downstream path didn't match base path");
        });
    }
}
