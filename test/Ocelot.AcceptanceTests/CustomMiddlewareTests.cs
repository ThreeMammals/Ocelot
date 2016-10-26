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
using Ocelot.Configuration.Yaml;
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
            _configurationPath = "configuration.yaml";
        }

        [Fact]
        public void response_should_come_from_pre_http_responder_middleware()
        {
            var configuration = new OcelotMiddlewareConfiguration
            {
                PreHttpResponderMiddleware = async (ctx, next) =>
                {
                    await ctx.Response.WriteAsync("PreHttpResponderMiddleware");
                }
            };

            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:41879/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get",
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration, _configurationPath))
                .And(x => _steps.GivenOcelotIsRunning(configuration))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("PreHttpResponderMiddleware"))
                .BDDfy();
        }

        [Fact]
        public void response_should_come_from_pre_http_requester_middleware()
        {
            var configuration = new OcelotMiddlewareConfiguration
            {
                PreHttpRequesterMiddleware = async (ctx, next) =>
                {
                    await ctx.Response.WriteAsync("PreHttpRequesterMiddleware");
                }
            };

            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:41879/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get",
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration, _configurationPath))
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
