namespace Ocelot.AcceptanceTests
{
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

    public class GitHubIssuesTests : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;
        private string _downstreamPath;

        public GitHubIssuesTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_fix_issue_238()
        {
        //{ "ReRoutes": [ { "DownstreamPathTemplate": "/api/values", "DownstreamScheme": "http", "DownstreamHostAndPorts": [ { "Host": "localhost", "Port": 8940 } ], "UpstreamPathTemplate": "/EquipmentInterfaces", "UpstreamHttpMethod": [ "Get" ] } ], "GlobalConfiguration": { "RequestIdKey": "OcRequestId", "AdministrationPath": "/administration" } }
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/values",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 8940,
                                }
                            },
                            UpstreamPathTemplate = "/EquipmentInterfaces",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = "OcRequestId",
                }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:8940", "/api/values", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/EquipmentInterfaces"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody)
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
                        _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                        if(_downstreamPath != basePath)
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