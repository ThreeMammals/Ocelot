using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests
{
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
        [Trait("PR", "570")]
        [Trait("Bug", "555")]
        public void should_fix_issue_555()
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
                            UpstreamHttpMethod = new List<string> { "Get" },
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

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/OcelotServiceApplication/OcelotApplicationService/a", 200, "Hello from Laura", "b=c"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/a?b=c"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_support_service_fabric_naming_and_dns_service_stateless_and_guest()
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
                            UpstreamHttpMethod = new List<string> { "Get" },
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

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/OcelotServiceApplication/OcelotApplicationService/api/values", 200, "Hello from Laura", "test=best"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/EquipmentInterfaces?test=best"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_support_service_fabric_naming_and_dns_service_statefull_and_actors()
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
                        UpstreamHttpMethod = new List<string> { "Get" },
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

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/OcelotServiceApplication/OcelotApplicationService/api/values", 200, "Hello from Laura", "PartitionKind=test&PartitionKey=1"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/EquipmentInterfaces?PartitionKind=test&PartitionKey=1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Theory]
        [Trait("PR", "722")]
        [Trait("Feat", "721")]
        [InlineData("/api/{version}/values", "/values", "Service_{version}/Api", "/Service_1.0/Api/values",   "/api/1.0/values?test=best",                "test=best")]
        [InlineData("/api/{version}/{all}",  "/{all}",  "Service_{version}/Api", "/Service_1.0/Api/products", "/api/1.0/products?test=the-best-from-Aly", "test=the-best-from-Aly")]
        public void should_support_placeholder_in_service_fabric_service_name(string upstream, string downstream, string serviceName, string downstreamUrl, string url, string query)
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = downstream,
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = upstream,
                            UpstreamHttpMethod = ["Get"],
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

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", downstreamUrl, 200, "Hello from Felix Boers", query))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(url))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Felix Boers"))
                .BDDfy();
        }

        [Fact]
        [Trait("PR", "722, 2032")]
        [Trait("Feat", "721")]
        public void should_throw_exception_when_config_is_invalid_for_ServiceFabricPlaceholdersInServiceName_rule()
        {
            // Arrange
            var port = PortFinder.GetRandomPort();
            var invalidConfig = new FileConfiguration
            {
                Routes =
                [
                    new()
                    {
                        DownstreamPathTemplate = "/{all}",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/api/{version}/{all}",
                        UpstreamHttpMethod = ["Get"],
                        ServiceName = "Service_{invalid}/Api", // invalid placeholder is not defined in upstream
                    },
                ],
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new()
                    {
                        Host = "localhost",
                        Port = port,
                        Type = "ServiceFabric",
                    },
                },
            };
            _steps.GivenThereIsAConfiguration(invalidConfig);

            // Act
            Exception exception = null;
            try
            {
                _steps.GivenOcelotIsRunning();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Assert
            exception.ShouldNotBeNull();
            exception.Message.ShouldBe("One or more errors occurred. (Unable to start Ocelot, errors are: SD Provider: ServiceFabric; Placeholders in ServiceName feature has invalid configuration! UpstreamPathTemplate '/api/{version}/{all}' doesn't contain the same placeholders in both DownstreamPathTemplate '/{all}' and ServiceName 'Service_{invalid}/Api', or vice versa!)");
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody, string expectedQueryString)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
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

        public void Dispose()
        {
            _serviceHandler?.Dispose();
            _steps.Dispose();
        }
    }
}
