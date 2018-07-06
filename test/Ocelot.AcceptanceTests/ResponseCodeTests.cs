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
    public class ResponseCodeTests : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;
        private string _downstreamPath;

        public ResponseCodeTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_response_304_when_service_returns_304()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/{everything}",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51879,
                                }
                            },
                            UpstreamPathTemplate = "/{everything}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879", "/inline.132.bundle.js", 304))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/inline.132.bundle.js"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotModified))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode)
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
                            context.Response.StatusCode = statusCode;
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
