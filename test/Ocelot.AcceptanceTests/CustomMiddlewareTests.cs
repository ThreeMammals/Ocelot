using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class CustomMiddlewareTests : IDisposable
    {
        private readonly string _configurationPath;
        private IWebHost _builder;
        private readonly Steps _steps;

        public CustomMiddlewareTests()
        {
            _steps = new Steps();;
            _configurationPath = "configuration.json";
        }

        [Fact]
        public void response_should_come_from_pre_authorisation_middleware()
        {
            var configuration = new OcelotMiddlewareConfiguration
            {
                PreAuthorisationMiddleware = async (ctx, next) =>
                {
                    await ctx.Response.WriteAsync("PreHttpResponderMiddleware");
                }
            };

            var fileConfiguration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamTemplate = "http://localhost:41879/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get",
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(fileConfiguration, _configurationPath))
                .And(x => _steps.GivenOcelotIsRunning(configuration))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("PreHttpResponderMiddleware"))
                .BDDfy();
        }

        [Fact]
        public void response_should_come_from_pre_http_authentication_middleware()
        {
            var configuration = new OcelotMiddlewareConfiguration
            {
                PreAuthenticationMiddleware = async (ctx, next) =>
                {
                    await ctx.Response.WriteAsync("PreHttpRequesterMiddleware");
                }
            };

            var fileConfiguration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamTemplate = "http://localhost:41879/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get",
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(fileConfiguration, _configurationPath))
                .And(x => _steps.GivenOcelotIsRunning(configuration))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("PreHttpRequesterMiddleware"))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode)
        {
            _builder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        context.Response.StatusCode = statusCode;
                        return Task.CompletedTask;
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
