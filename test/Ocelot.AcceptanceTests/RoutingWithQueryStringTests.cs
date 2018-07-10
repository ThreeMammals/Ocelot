using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class RoutingWithQueryStringTests : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;
        private string _downstreamPath;

        public RoutingWithQueryStringTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_response_200_with_query_string_template()
        {
            var subscriptionId = Guid.NewGuid().ToString();
            var unitId = Guid.NewGuid().ToString();

            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 61879,
                                }
                            },
                            UpstreamPathTemplate = "/api/units/{subscriptionId}/{unitId}/updates",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:61879", $"/api/subscriptions/{subscriptionId}/updates", $"?unitId={unitId}", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway($"/api/units/{subscriptionId}/{unitId}/updates"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, string queryString, int statusCode, string responseBody)
        {
            _builder = new WebHostBuilder()
                .UseUrls(baseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.UsePathBase(basePath);
                    app.Run(async context =>
                    {   

                        if(context.Request.PathBase.Value != basePath || context.Request.QueryString.Value != queryString)
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsync("downstream path didnt match base path");
                        }
                        else
                        {
                            context.Response.StatusCode = statusCode;
                            await context.Response.WriteAsync(responseBody);
                        }
                    });
                })
                .Build();

            _builder.Start();
        }

        internal void ThenTheDownstreamUrlPathShouldBe(string expectedDownstreamPath)
        {
            _downstreamPath.ShouldBe(expectedDownstreamPath);
        }

        public void Dispose()
        {
            _builder?.Dispose();
            _steps.Dispose();
        }
    }
}
