using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

public sealed class ServiceFabricTests : Steps
{
    private string _downstreamPath;

    public ServiceFabricTests()
    {
    }

    [Fact]
    [Trait("PR", "570")]
    [Trait("Bug", "555")]
    public void Should_fix_issue_555()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/{everything}",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/{everything}",
                        UpstreamHttpMethod = ["Get"],
                        ServiceName = "OcelotServiceApplication/OcelotApplicationService",
                    },
                },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Host = "localhost",
                    Port = port,
                    Type = "ServiceFabric",
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/OcelotServiceApplication/OcelotApplicationService/a", 200, "Hello from Laura", "b=c"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/a?b=c"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_support_service_fabric_naming_and_dns_service_stateless_and_guest()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/values",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/EquipmentInterfaces",
                        UpstreamHttpMethod = ["Get"],
                        ServiceName = "OcelotServiceApplication/OcelotApplicationService",
                    },
                },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Host = "localhost",
                    Port = port,
                    Type = "ServiceFabric",
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/OcelotServiceApplication/OcelotApplicationService/api/values", 200, "Hello from Laura", "test=best"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/EquipmentInterfaces?test=best"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_support_service_fabric_naming_and_dns_service_statefull_and_actors()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/api/values",
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/EquipmentInterfaces",
                    UpstreamHttpMethod = ["Get"],
                    ServiceName = "OcelotServiceApplication/OcelotApplicationService",
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Host = "localhost",
                    Port = port,
                    Type = "ServiceFabric",
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/OcelotServiceApplication/OcelotApplicationService/api/values", 200, "Hello from Laura", "PartitionKind=test&PartitionKey=1"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/EquipmentInterfaces?PartitionKind=test&PartitionKey=1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Theory]
    [Trait("PR", "722")]
    [Trait("Feat", "721")]
    [InlineData("/api/{version}/values", "/values", "Service_{version}/Api", "/Service_1.0/Api/values", "/api/1.0/values?test=best", "test=best")]
    [InlineData("/api/{version}/{all}", "/{all}", "Service_{version}/Api", "/Service_1.0/Api/products", "/api/1.0/products?test=the-best-from-Aly", "test=the-best-from-Aly")]
    public void should_support_placeholder_in_service_fabric_service_name(string upstream, string downstream, string serviceName, string downstreamUrl, string url, string query)
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new()
                {
                    new()
                    {
                        DownstreamPathTemplate = downstream,
                        DownstreamScheme = Uri.UriSchemeHttp,
                        UpstreamPathTemplate = upstream,
                        UpstreamHttpMethod = [HttpMethods.Get],
                        ServiceName = serviceName,
                    },
                },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Host = "localhost",
                    Port = port,
                    Type = "ServiceFabric",
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, downstreamUrl, 200, "Hello from Felix Boers", query))
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
