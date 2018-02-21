﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 41879,
                                }
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200, ""))
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
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 41879,
                                }
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200, ""))
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
                            DownstreamPathTemplate = "/41879/",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 41879,
                                }
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200, ""))
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
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 41879,
                                }
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200, ""))
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
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 41879,
                                }
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200, ""))
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
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 41879,
                                }
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41879", 200, ""))
                .And(x => _steps.GivenThereIsAConfiguration(fileConfiguration, _configurationPath))
                .And(x => _steps.GivenOcelotIsRunning(configuration))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheCounterIs(1))
                .BDDfy();
        }

        
        [Fact(Skip = "This is just an example to show how you could hook into Ocelot pipeline with your own middleware. At the moment you must use Response.OnCompleted callback and cannot change the response :( I will see if this can be changed one day!")]
        public void should_fix_issue_237()
        {
            Func<object, Task> callback = state =>
            {
                var httpContext = (HttpContext)state;

                if (httpContext.Response.StatusCode > 400)
                {                    
                    Debug.WriteLine("COUNT CALLED");
                    Console.WriteLine("COUNT CALLED");
                }

                return Task.CompletedTask;
            };

            var fileConfiguration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/west",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 41880,
                                }
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:41880", 200, "/test"))
                .And(x => _steps.GivenThereIsAConfiguration(fileConfiguration, _configurationPath))
                .And(x => _steps.GivenOcelotIsRunningWithMiddleareBeforePipeline<FakeMiddleware>(callback))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        private void ThenTheCounterIs(int expected)
        {
            _counter.ShouldBe(expected);
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode, string basePath)
        {
            _builder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.UsePathBase(basePath);
                    app.Run(context =>
                    {

                        if(string.IsNullOrEmpty(basePath))
                        {
                            context.Response.StatusCode = statusCode;
                        }
                        else if(context.Request.Path.Value != basePath)
                        {
                            context.Response.StatusCode = 404;;
                        }

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

        public class FakeMiddleware
        {
            private readonly RequestDelegate _next;
            private readonly Func<object, Task> _callback; 
            
            public FakeMiddleware(RequestDelegate next, Func<object, Task> callback)
            {
                _next = next;
                _callback = callback;
            }

            public async Task Invoke(HttpContext context)
            {
                await _next(context);
                
                context.Response.OnCompleted(_callback, context);
            }
        }
    }
}
