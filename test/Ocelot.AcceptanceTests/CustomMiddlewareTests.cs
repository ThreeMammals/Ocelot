using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration.Yaml;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.ScopedData;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class CustomMiddlewareTests : IDisposable
    {
        private TestServer _server;
        private HttpClient _client;
        private HttpResponseMessage _response;
        private readonly string _configurationPath;
        private IWebHost _builder;
        private readonly Steps _steps;

        public CustomMiddlewareTests()
        {
            _steps = new Steps();;
            _configurationPath = $"configuration.yaml";
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

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(new YamlConfiguration
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
                }, _configurationPath))
                .And(x => x.GivenOcelotIsRunning(configuration))
                .When(x => x.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => x.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheResponseBodyShouldBe("PreHttpResponderMiddleware"))
                .BDDfy();
        }

        [Fact]
        public void response_should_come_from_pre_http_requester_middleware()
        {
            var configuration = new OcelotMiddlewareConfiguration
            {
                PreHttpRequesterMiddleware = async (ctx, next) =>
                {
                    var service = ctx.RequestServices.GetService<IScopedRequestDataRepository>();
                    service.Add("Response", new HttpResponseMessage {Content = new StringContent("PreHttpRequesterMiddleware")});
                }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(new YamlConfiguration
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
                }, _configurationPath))
                .And(x => x.GivenOcelotIsRunning(configuration))
                .When(x => x.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => x.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheResponseBodyShouldBe("PreHttpRequesterMiddleware"))
                .BDDfy();
        }



        /// <summary>
        /// This is annoying cos it should be in the constructor but we need to set up the yaml file before calling startup so its a step.
        /// </summary>
        private void GivenOcelotIsRunning(OcelotMiddlewareConfiguration ocelotMiddlewareConfig)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddYamlFile("configuration.yaml")
                .AddEnvironmentVariables();

            var configuration = builder.Build();

            _server = new TestServer(new WebHostBuilder()
                .UseConfiguration(configuration)
                .ConfigureServices(s =>
                {
                    s.AddOcelotYamlConfiguration(configuration);
                    s.AddOcelot();
                })
                .ConfigureLogging(l =>
                {
                    l.AddConsole(configuration.GetSection("Logging"));
                    l.AddDebug();
                })
                .Configure(a =>
                {
                    a.UseOcelot(ocelotMiddlewareConfig);
                }));
                
            _client = _server.CreateClient();
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
                    app.Run(async context =>
                    {
                        context.Response.StatusCode = statusCode;
                    });
                })
                .Build();

            _builder.Start();
        }

        private void WhenIGetUrlOnTheApiGateway(string url)
        {
            _response = _client.GetAsync(url).Result;
        }

        private void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        private void ThenTheResponseBodyShouldBe(string expectedBody)
        {
            _response.Content.ReadAsStringAsync().Result.ShouldBe(expectedBody);
        }

        public void Dispose()
        {
            _builder?.Dispose();
            _client.Dispose();
            _server.Dispose();
        }
    }
}
