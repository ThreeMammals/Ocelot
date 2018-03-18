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
using Butterfly.Client.AspNetCore;
using static Rafty.Infrastructure.Wait;

namespace Ocelot.AcceptanceTests
{
    public class ButterflyTracingTests : IDisposable
    {
        private IWebHost _serviceOneBuilder;
        private IWebHost _serviceTwoBuilder;
        private IWebHost _fakeButterfly;
        private readonly Steps _steps;
        private string _downstreamPathOne;
        private string _downstreamPathTwo;
        private int _butterflyCalled;

        public ButterflyTracingTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_forward_tracing_information_from_ocelot_and_downstream_services()
        {
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
                                    Port = 51887,
                                }
                            },
                            UpstreamPathTemplate = "/api001/values",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            HttpHandlerOptions = new FileHttpHandlerOptions
                            {
                                UseTracing = true
                            },
                            QoSOptions = new FileQoSOptions
                            {
                                ExceptionsAllowedBeforeBreaking = 3,
                                DurationOfBreak = 10,
                                TimeoutValue = 5000
                            }
                        },
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/values",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51388,
                                }
                            },
                            UpstreamPathTemplate = "/api002/values",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            HttpHandlerOptions = new FileHttpHandlerOptions
                            {
                                UseTracing = true
                            },
                            QoSOptions = new FileQoSOptions
                            {
                                ExceptionsAllowedBeforeBreaking = 3,
                                DurationOfBreak = 10,
                                TimeoutValue = 5000
                            }
                        }
                    }
            };

            var butterflyUrl = "http://localhost:9618";

            this.Given(x => GivenServiceOneIsRunning("http://localhost:51887", "/api/values", 200, "Hello from Laura", butterflyUrl))
                .And(x => GivenServiceTwoIsRunning("http://localhost:51388", "/api/values", 200, "Hello from Tom", butterflyUrl))
                .And(x => GivenFakeButterfly(butterflyUrl))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningUsingButterfly(butterflyUrl))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/api001/values"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                 .When(x => _steps.WhenIGetUrlOnTheApiGateway("/api002/values"))
                 .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                 .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Tom"))
                .BDDfy();

            var commandOnAllStateMachines = WaitFor(5000).Until(() => _butterflyCalled == 4);

            commandOnAllStateMachines.ShouldBeTrue();
        }

        [Fact]
        public void should_return_tracing_header()
        {
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
                                    Port = 51387,
                                }
                            },
                            UpstreamPathTemplate = "/api001/values",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            HttpHandlerOptions = new FileHttpHandlerOptions
                            {
                                UseTracing = true
                            },
                            QoSOptions = new FileQoSOptions
                            {
                                ExceptionsAllowedBeforeBreaking = 3,
                                DurationOfBreak = 10,
                                TimeoutValue = 5000
                            },
                            DownstreamHeaderTransform = new Dictionary<string, string>()
                            {
                                {"Trace-Id", "{TraceId}"},
                                {"Tom", "Laura"}
                            }
                        }
                    }
            };

            var butterflyUrl = "http://localhost:9618";

            this.Given(x => GivenServiceOneIsRunning("http://localhost:51387", "/api/values", 200, "Hello from Laura", butterflyUrl))
                .And(x => GivenFakeButterfly(butterflyUrl))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningUsingButterfly(butterflyUrl))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/api001/values"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => _steps.ThenTheTraceHeaderIsSet("Trace-Id"))
                .And(x => _steps.ThenTheResponseHeaderIs("Tom", "Laura"))
                .BDDfy();
        }

        private void GivenServiceOneIsRunning(string baseUrl, string basePath, int statusCode, string responseBody, string butterflyUrl)
        {
            _serviceOneBuilder = new WebHostBuilder()
                .UseUrls(baseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .ConfigureServices(services => {
                    services.AddButterfly(option =>
                    {
                        option.CollectorUrl = butterflyUrl;
                        option.Service = "Service One";
                        option.IgnoredRoutesRegexPatterns = new string[0];
                    });
                })
                .Configure(app =>
                {
                    app.UsePathBase(basePath);
                    app.Run(async context =>
                    {   
                        _downstreamPathOne = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                        if(_downstreamPathOne != basePath)
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

            _serviceOneBuilder.Start();
        }

        private void GivenFakeButterfly(string baseUrl)
        {
            _fakeButterfly = new WebHostBuilder()
                .UseUrls(baseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        _butterflyCalled++;
                        await context.Response.WriteAsync("OK...");
                    });
                })
                .Build();

            _fakeButterfly.Start();
        }

        private void GivenServiceTwoIsRunning(string baseUrl, string basePath, int statusCode, string responseBody, string butterflyUrl)
        {
            _serviceTwoBuilder = new WebHostBuilder()
                .UseUrls(baseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .ConfigureServices(services => {
                    services.AddButterfly(option =>
                    {
                        option.CollectorUrl = butterflyUrl;
                        option.Service = "Service Two";
                        option.IgnoredRoutesRegexPatterns = new string[0];
                    });
                })
                .Configure(app =>
                {
                    app.UsePathBase(basePath);
                    app.Run(async context =>
                    {   
                        _downstreamPathTwo = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                        if(_downstreamPathTwo != basePath)
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

            _serviceTwoBuilder.Start();
        }

        internal void ThenTheDownstreamUrlPathShouldBe(string expectedDownstreamPathOne, string expectedDownstreamPath)
        {
            _downstreamPathOne.ShouldBe(expectedDownstreamPathOne);
            _downstreamPathTwo.ShouldBe(expectedDownstreamPath);
        }

        public void Dispose()
        {
            _serviceOneBuilder?.Dispose();
            _serviceTwoBuilder?.Dispose();
            _fakeButterfly?.Dispose();
            _steps.Dispose();
        }
    }
}
