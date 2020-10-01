namespace Ocelot.AcceptanceTests
{
    using Ocelot.Configuration.File;
    using Consul;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using TestStack.BDDfy;
    using Xunit;

    public class TwoDownstreamServicesTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly List<ServiceEntry> _serviceEntries;
        private string _downstreamPathOne;
        private string _downstreamPathTwo;
        private readonly ServiceHandler _serviceHandler;

        public TwoDownstreamServicesTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
            _serviceEntries = new List<ServiceEntry>();
        }

        [Fact]
        public void should_fix_issue_194()
        {
            var consulPort = RandomPortFinder.GetRandomPort();
            var servicePort1 = RandomPortFinder.GetRandomPort();
            var servicePort2 = RandomPortFinder.GetRandomPort();
            var downstreamServiceOneUrl = $"http://localhost:{servicePort1}";
            var downstreamServiceTwoUrl = $"http://localhost:{servicePort2}";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterOneId,
                        DownstreamPathTemplate = "/api/user/{user}",
                        UpstreamPathTemplate = "/api/user/{user}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                    new FileRoute
                    {
                        ClusterId = _steps.ClusterTwoId,
                        DownstreamPathTemplate = "/api/product/{product}",
                        UpstreamPathTemplate = "/api/product/{product}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_steps.ClusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterOneId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{servicePort1}",
                                    }
                                },
                            },
                        }
                    },
                    {_steps.ClusterTwoId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_steps.ClusterTwoId}/destination1", new FileDestination
                                    {
                                        Address = $"http://localhost:{servicePort2}",
                                    }
                                },
                            },
                        }
                    },
                },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Scheme = "https",
                        Host = "localhost",
                        Port = consulPort,
                    },
                },
            };

            this.Given(x => x.GivenProductServiceOneIsRunning(downstreamServiceOneUrl, "/api/user/info", 200, "user"))
                .And(x => x.GivenProductServiceTwoIsRunning(downstreamServiceTwoUrl, "/api/product/info", 200, "product"))
                .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/api/user/info?id=1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("user"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/api/product/info?id=1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("product"))
                .BDDfy();
        }

        private void GivenThereIsAFakeConsulServiceDiscoveryProvider(string url)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
            {
                if (context.Request.Path.Value == "/v1/health/service/product")
                {
                    var json = JsonConvert.SerializeObject(_serviceEntries);
                    context.Response.Headers.Add("Content-Type", "application/json");
                    await context.Response.WriteAsync(json);
                }
            });
        }

        private void GivenProductServiceOneIsRunning(string baseUrl, string basePath, int statusCode, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                _downstreamPathOne = !string.IsNullOrEmpty(context.Request.PathBase.Value)
                    ? context.Request.PathBase.Value
                    : context.Request.Path.Value;

                if (_downstreamPathOne != basePath)
                {
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync("downstream path didnt match base path");
                }
                else
                {
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync(responseBody);
                }
            });
        }

        private void GivenProductServiceTwoIsRunning(string baseUrl, string basePath, int statusCode, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                _downstreamPathTwo = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                if (_downstreamPathTwo != basePath)
                {
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync("downstream path didnt match base path");
                }
                else
                {
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync(responseBody);
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
