using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Ocelot.Configuration.File;
using Ocelot.Middleware;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class CustomMiddlewareTests : IDisposable
    {
        private readonly string _configurationPath;
        private IWebHost _builder;
        private readonly Steps _steps;
        private int _counter;

        public CustomMiddlewareTests()
        {
            _counter = 0;
            _steps = new Steps();;
            _configurationPath = "configuration.json";
        }

        [Fact]
        public void should_call_pre_query_string_builder_middleware()
        {
            var configuration = new OcelotMiddlewareConfiguration
            {
                AuthorisationMiddleware = async (ctx, next) =>
                {
                    _counter++;
                    await next.Invoke();
                }
            };

            var fileConfiguration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamPort = 41879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = "Get",
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(fileConfiguration, _configurationPath))
                .And(x => _steps.GivenOcelotIsRunning(configuration))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheCounterIs(1))
                .BDDfy();
        }

        [Fact]
        public void should_call_authorisation_middleware()
        {
            var configuration = new OcelotMiddlewareConfiguration
            {
                AuthorisationMiddleware = async (ctx, next) =>
                {
                    _counter++;
                    await next.Invoke();
                }
            };

            var fileConfiguration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamPort = 41879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = "Get",

                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(fileConfiguration, _configurationPath))
                .And(x => _steps.GivenOcelotIsRunning(configuration))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheCounterIs(1))
                .BDDfy();
        }

        [Fact]
        public void should_call_authentication_middleware()
        {
            var configuration = new OcelotMiddlewareConfiguration
            {
                AuthenticationMiddleware = async (ctx, next) =>
                {
                    _counter++;
                    await next.Invoke();
                }
            };

            var fileConfiguration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "41879/",
                            DownstreamPort = 41879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = "Get",

                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(fileConfiguration, _configurationPath))
                .And(x => _steps.GivenOcelotIsRunning(configuration))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheCounterIs(1))
                .BDDfy();
        }

        [Fact]
        public void should_call_pre_error_middleware()
        {
            var configuration = new OcelotMiddlewareConfiguration
            {
                PreErrorResponderMiddleware = async (ctx, next) =>
                {
                    _counter++;
                    await next.Invoke();
                }
            };

            var fileConfiguration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamPort = 41879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = "Get",
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(fileConfiguration, _configurationPath))
                .And(x => _steps.GivenOcelotIsRunning(configuration))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheCounterIs(1))
                .BDDfy();
        }

        [Fact]
        public void should_call_pre_authorisation_middleware()
        {
            var configuration = new OcelotMiddlewareConfiguration
            {
                PreAuthorisationMiddleware = async (ctx, next) =>
                {
                    _counter++;
                    await next.Invoke();
                }
            };

            var fileConfiguration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamPort = 41879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = "Get",
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(fileConfiguration, _configurationPath))
                .And(x => _steps.GivenOcelotIsRunning(configuration))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheCounterIs(1))
                .BDDfy();
        }

        [Fact]
        public void should_call_pre_http_authentication_middleware()
        {
            var configuration = new OcelotMiddlewareConfiguration
            {
                PreAuthenticationMiddleware = async (ctx, next) =>
                {
                    _counter++;
                    await next.Invoke();
                }
            };

            var fileConfiguration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamPort = 41879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = "Get",
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200))
                .And(x => _steps.GivenThereIsAConfiguration(fileConfiguration, _configurationPath))
                .And(x => _steps.GivenOcelotIsRunning(configuration))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheCounterIs(1))
                .BDDfy();
        }

        private void ThenTheCounterIs(int expected)
        {
            _counter.ShouldBe(expected);
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
