using Microsoft.AspNetCore.Http;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

[Trait("Feat", "238")] // https://github.com/ThreeMammals/Ocelot/issues/238
public sealed class ServiceFabricTests : Steps
{
    private string _downstreamPath;

    [Fact]
    [Trait("PR", "570")] // https://github.com/ThreeMammals/Ocelot/pull/570
    [Trait("Bug", "555")] // https://github.com/ThreeMammals/Ocelot/issues/555
    public void Should_fix_issue_555()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenCatchAllRoute(port);
        route.ServiceName = "OcelotServiceApplication/OcelotApplicationService";
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Host = "localhost",
            Port = port,
            Type = "ServiceFabric",
        };
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, "/OcelotServiceApplication/OcelotApplicationService/a", 200, "Hello from Laura", "b=c"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/a?b=c"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
        .BDDfy();
    }

    [Fact]
    [Trait("PR", "242")] // https://github.com/ThreeMammals/Ocelot/pull/242
    public void Should_support_service_fabric_naming_and_dns_service_stateless_and_guest()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/EquipmentInterfaces", "/api/values");
        route.ServiceName = "OcelotServiceApplication/OcelotApplicationService";
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Host = "localhost",
            Port = port,
            Type = "ServiceFabric",
        };
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, "/OcelotServiceApplication/OcelotApplicationService/api/values", 200, "Hello from Laura", "test=best"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/EquipmentInterfaces?test=best"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
        .BDDfy();
    }

    [Fact]
    [Trait("PR", "242")] // https://github.com/ThreeMammals/Ocelot/pull/242
    public void Should_support_service_fabric_naming_and_dns_service_statefull_and_actors()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/EquipmentInterfaces", "/api/values");
        route.ServiceName = "OcelotServiceApplication/OcelotApplicationService";
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Host = "localhost",
            Port = port,
            Type = "ServiceFabric",
        };
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, "/OcelotServiceApplication/OcelotApplicationService/api/values", 200, "Hello from Laura", "PartitionKind=test&PartitionKey=1"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/EquipmentInterfaces?PartitionKind=test&PartitionKey=1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
        .BDDfy();
    }

    [Theory]
    [Trait("PR", "722")] // https://github.com/ThreeMammals/Ocelot/pull/722
    [Trait("Feat", "721")] // https://github.com/ThreeMammals/Ocelot/issues/721
    [InlineData("/api/{version}/values", "/values", "Service_{version}/Api", "/Service_1.0/Api/values", "/api/1.0/values?test=best", "test=best")]
    [InlineData("/api/{version}/{all}", "/{all}", "Service_{version}/Api", "/Service_1.0/Api/products", "/api/1.0/products?test=the-best-from-Aly", "test=the-best-from-Aly")]
    public void Should_support_placeholder_in_service_fabric_service_name(string upstream, string downstream, string serviceName, string downstreamUrl, string url, string query)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, upstream, downstream);
        route.ServiceName = serviceName;
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Host = "localhost",
            Port = port,
            Type = "ServiceFabric",
        };
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, downstreamUrl, 200, "Hello from Felix Boers", query))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway(url))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Felix Boers"))
        .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, int statusCode, string responseBody, string expectedQueryString)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, async context =>
        {
            _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

            if (_downstreamPath != basePath)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync("downstream path didnt match base path");
            }
            else
            {
                if (context.Request.QueryString.Value.Contains(expectedQueryString))
                {
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync(responseBody);
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    await context.Response.WriteAsync("downstream path didnt match base path");
                }
            }
        });
    }
}
