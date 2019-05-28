namespace Ocelot.AcceptanceTests
{
    using Configuration.File;
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
            var consulPort = 8503;
            var downstreamServiceOneUrl = "http://localhost:8362";
            var downstreamServiceTwoUrl = "http://localhost:8330";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";

            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/user/{user}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 8362,
                                }
                            },
                            UpstreamPathTemplate = "/api/user/{user}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        },
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/product/{product}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 8330,
                                }
                            },
                            UpstreamPathTemplate = "/api/product/{product}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = consulPort
                    }
                }
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
