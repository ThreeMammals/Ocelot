using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests
{
    public class RoutingWithQueryStringTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly ServiceHandler _serviceHandler;

        public RoutingWithQueryStringTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
        }

        [Fact]
        public void Should_return_response_200_with_query_string_template()
        {
            var subscriptionId = Guid.NewGuid().ToString();
            var unitId = Guid.NewGuid().ToString();
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/api/units/{subscriptionId}/{unitId}/updates",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", $"/api/subscriptions/{subscriptionId}/updates", $"?unitId={unitId}", "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/api/units/{subscriptionId}/{unitId}/updates"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Theory(DisplayName = "1182: " + nameof(Should_return_200_with_query_string_template_different_keys))]
        [InlineData("")]
        [InlineData("&x=xxx")]
        public void Should_return_200_with_query_string_template_different_keys(string additionalParams)
        {
            var subscriptionId = Guid.NewGuid().ToString();
            var unitId = Guid.NewGuid().ToString();
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/subscriptions/{subscriptionId}/updates?unitId={unit}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/api/units/{subscriptionId}/updates?unit={unit}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", $"/api/subscriptions/{subscriptionId}/updates", $"?unitId={unitId}{additionalParams}", "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/api/units/{subscriptionId}/updates?unit={unitId}{additionalParams}"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Theory(DisplayName = "1174: " + nameof(Should_return_200_and_forward_query_parameters_without_duplicates))]
        [InlineData("projectNumber=45&startDate=2019-12-12&endDate=2019-12-12", "endDate=2019-12-12&projectNumber=45&startDate=2019-12-12")]
        [InlineData("$filter=ProjectNumber eq 45 and DateOfSale ge 2020-03-01T00:00:00z and DateOfSale le 2020-03-15T00:00:00z", "$filter=ProjectNumber%20eq%2045%20and%20DateOfSale%20ge%202020-03-01T00:00:00z%20and%20DateOfSale%20le%202020-03-15T00:00:00z")]
        public void Should_return_200_and_forward_query_parameters_without_duplicates(string everythingelse, string expectedOrdered)
        {
            var port = PortFinder.GetRandomPort();
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/contracts?{everythingelse}",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new() { Host = "localhost", Port = port },
                        },
                        UpstreamPathTemplate = "/contracts?{everythingelse}",
                        UpstreamHttpMethod = new() { "Get" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", $"/api/contracts", $"?{expectedOrdered}", "Hello from @sunilk3"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/contracts?{everythingelse}"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from @sunilk3"))
                .BDDfy();
        }

        [Fact]
        public void Should_return_response_200_with_odata_query_string()
        {
            var subscriptionId = Guid.NewGuid().ToString();
            var unitId = Guid.NewGuid().ToString();
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/{everything}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            UpstreamPathTemplate = "/{everything}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/odata/customers", "?$filter=Name%20eq%20'Sam'", "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/odata/customers?$filter=Name eq 'Sam' "))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void Should_return_response_200_with_query_string_upstream_template()
        {
            var subscriptionId = Guid.NewGuid().ToString();
            var unitId = Guid.NewGuid().ToString();
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/units/{subscriptionId}/{unitId}/updates",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", $"/api/units/{subscriptionId}/{unitId}/updates", string.Empty, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates?unitId={unitId}"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void Should_return_response_404_with_query_string_upstream_template_no_query_string()
        {
            var subscriptionId = Guid.NewGuid().ToString();
            var unitId = Guid.NewGuid().ToString();
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/units/{subscriptionId}/{unitId}/updates",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", $"/api/units/{subscriptionId}/{unitId}/updates", string.Empty, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void Should_return_response_404_with_query_string_upstream_template_different_query_string()
        {
            var subscriptionId = Guid.NewGuid().ToString();
            var unitId = Guid.NewGuid().ToString();
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/units/{subscriptionId}/{unitId}/updates",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", $"/api/units/{subscriptionId}/{unitId}/updates", string.Empty, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates?test=1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void Should_return_response_200_with_query_string_upstream_template_multiple_params()
        {
            var subscriptionId = Guid.NewGuid().ToString();
            var unitId = Guid.NewGuid().ToString();
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/units/{subscriptionId}/{unitId}/updates",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", $"/api/units/{subscriptionId}/{unitId}/updates", "?productId=1", "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/api/subscriptions/{subscriptionId}/updates?unitId={unitId}&productId=1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        /// <summary>
        /// To reproduce 1288: query string should contain the placeholder name and value.
        /// </summary>
        [Fact(DisplayName = "1288: " + nameof(Should_copy_query_string_to_downstream_path))]
        public void Should_copy_query_string_to_downstream_path()
        {
            var idName = "id";
            var idValue = "3";
            var queryName = idName + "1";
            var queryValue = "2" + idValue + "12";
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new FileRoute
                        {
                            DownstreamPathTemplate = $"/cpx/t1/{{{idName}}}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new() { Host = "localhost", Port = port },
                            },
                            UpstreamPathTemplate = $"/safe/{{{idName}}}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", $"/cpx/t1/{idValue}", $"?{queryName}={queryValue}", "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/safe/{idValue}?{queryName}={queryValue}"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, string queryString, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                if ((context.Request.PathBase.Value != basePath) || context.Request.QueryString.Value != queryString)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("downstream path didnt match base path");
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync(responseBody);
                }
            });
        }

        public void Dispose()
        {
            _serviceHandler?.Dispose();
            _steps.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
