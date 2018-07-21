namespace Ocelot.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Configuration.File;
    using TestStack.BDDfy;
    using Xunit;

    public class ServiceFabricTests : IDisposable
    {
        private readonly Steps _steps;
        private string _downstreamPath;
        private readonly ServiceHandler _serviceHandler;

        public ServiceFabricTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
        }

        [Fact]
        public void should_support_service_fabric_naming_and_dns_service_stateless_and_guest()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/values",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/EquipmentInterfaces",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            UseServiceDiscovery = true,
                            ServiceName = "OcelotServiceApplication/OcelotApplicationService"
                        }
                    },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = 19081,
                        Type = "ServiceFabric"
                    }
                }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:19081", "/OcelotServiceApplication/OcelotApplicationService/api/values", 200, "Hello from Laura", "cmd=instance"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/EquipmentInterfaces"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_support_service_fabric_naming_and_dns_service_statefull_and_actors()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/values",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/EquipmentInterfaces",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        UseServiceDiscovery = true,
                        ServiceName = "OcelotServiceApplication/OcelotApplicationService"
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = 19081,
                        Type = "ServiceFabric"
                    }
                }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:19081", "/OcelotServiceApplication/OcelotApplicationService/api/values", 200, "Hello from Laura", "PartitionKind=test&PartitionKey=1"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/EquipmentInterfaces?PartitionKind=test&PartitionKey=1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody, string expectedQueryString)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                if (_downstreamPath != basePath)
                {
                    context.Response.StatusCode = statusCode;
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
                        context.Response.StatusCode = statusCode;
                        await context.Response.WriteAsync("downstream path didnt match base path");
                    }
                }
            });
        }

        public void Dispose()
        {
            _serviceHandler?.Dispose();
            _steps.Dispose();
        }
    }
}
