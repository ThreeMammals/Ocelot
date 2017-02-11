using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class CaseSensitiveRoutingTests : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;

        public CaseSensitiveRoutingTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_response_200_when_global_ignore_case_sensitivity_set()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/products/{productId}",
                            DownstreamPort = 51879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/products/{productId}",
                            UpstreamHttpMethod = "Get",
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879/api/products/1", 200, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_when_reroute_ignore_case_sensitivity_set()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/products/{productId}",
                            DownstreamPort = 51879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/products/{productId}",
                            UpstreamHttpMethod = "Get",
                            ReRouteIsCaseSensitive = false,
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879/api/products/1", 200, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_404_when_reroute_respect_case_sensitivity_set()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/products/{productId}",
                            DownstreamPort = 51879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/products/{productId}",
                            UpstreamHttpMethod = "Get",
                            ReRouteIsCaseSensitive = true,
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879/api/products/1", 200, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_when_reroute_respect_case_sensitivity_set()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/products/{productId}",
                            DownstreamPort = 51879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/PRODUCTS/{productId}",
                            UpstreamHttpMethod = "Get",
                            ReRouteIsCaseSensitive = true,
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879/api/products/1", 200, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_404_when_global_respect_case_sensitivity_set()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/products/{productId}",
                            DownstreamPort = 51879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/products/{productId}",
                            UpstreamHttpMethod = "Get",
                            ReRouteIsCaseSensitive = true,
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879/api/products/1", 200, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_when_global_respect_case_sensitivity_set()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/products/{productId}",
                            DownstreamPort = 51879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/PRODUCTS/{productId}",
                            UpstreamHttpMethod = "Get",
                            ReRouteIsCaseSensitive = true,
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879/api/products/1", 200, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode, string responseBody)
        {
            _builder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.StatusCode = statusCode;
                        await context.Response.WriteAsync(responseBody);
                    });
                })
                .Build();

            _builder.Start();
        }

        public void Dispose()
        {
            _builder?.Dispose();
            _steps.Dispose();
        }
    }
}
