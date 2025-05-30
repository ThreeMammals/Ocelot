﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Ocelot.Configuration.File;
using Ocelot.Middleware;
using System.Diagnostics;

namespace Ocelot.AcceptanceTests;

public class CustomMiddlewareTests : Steps
{
    private int _counter;

    public CustomMiddlewareTests()
    {
        _counter = 0;
    }

    [Fact]
    public void Should_call_pre_query_string_builder_middleware()
    {
        var configuration = new OcelotPipelineConfiguration
        {
            AuthorizationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();
        var fileConfiguration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(fileConfiguration))
            .And(x => GivenOcelotIsRunning(configuration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_call_authorization_middleware()
    {
        var configuration = new OcelotPipelineConfiguration
        {
            AuthorizationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();

        var fileConfiguration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(fileConfiguration))
            .And(x => GivenOcelotIsRunning(configuration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_call_authentication_middleware()
    {
        var configuration = new OcelotPipelineConfiguration
        {
            AuthenticationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();

        var fileConfiguration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/41879/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(fileConfiguration))
            .And(x => GivenOcelotIsRunning(configuration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_call_pre_error_middleware()
    {
        var configuration = new OcelotPipelineConfiguration
        {
            PreErrorResponderMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();

        var fileConfiguration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(fileConfiguration))
            .And(x => GivenOcelotIsRunning(configuration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_call_pre_authorization_middleware()
    {
        var configuration = new OcelotPipelineConfiguration
        {
            PreAuthorizationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();

        var fileConfiguration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(fileConfiguration))
            .And(x => GivenOcelotIsRunning(configuration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_call_pre_http_authentication_middleware()
    {
        var configuration = new OcelotPipelineConfiguration
        {
            PreAuthenticationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();
        var fileConfiguration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(fileConfiguration))
            .And(x => GivenOcelotIsRunning(configuration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_not_throw_when_pipeline_terminates_early()
    {
        var configuration = new OcelotPipelineConfiguration
        {
            PreQueryStringBuilderMiddleware = (context, next) =>
                Task.Run(() =>
                {
                    _counter++;
                    return; // do not invoke the rest of the pipeline
                }),
        };

        var port = PortFinder.GetRandomPort();

        var fileConfiguration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, ""))
            .And(x => GivenThereIsAConfiguration(fileConfiguration))
            .And(x => GivenOcelotIsRunning(configuration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact(Skip = "This is just an example to show how you could hook into Ocelot pipeline with your own middleware. At the moment you must use Response.OnCompleted callback and cannot change the response :( I will see if this can be changed one day!")]
    public void Should_fix_issue_237()
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

        var port = PortFinder.GetRandomPort();
        var fileConfiguration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/west",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/test"))
            .And(x => GivenThereIsAConfiguration(fileConfiguration))
            .And(x => GivenOcelotIsRunningWithMiddlewareBeforePipeline<FakeMiddleware>(callback))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    private void GivenOcelotIsRunningWithMiddlewareBeforePipeline<T>(Func<object, Task> callback)
    {
        var builder = TestHostBuilder.Create()
            .ConfigureAppConfiguration(WithBasicConfiguration)
            .ConfigureServices(WithAddOcelot)
            .Configure(async app =>
            {
                app.UseMiddleware<T>(callback);
                await app.UseOcelot();
            });
        ocelotServer = new TestServer(builder);
        ocelotClient = ocelotServer.CreateClient();
    }

    private void ThenTheCounterIs(int expected)
    {
        _counter.ShouldBe(expected);
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath)
    {
        handler.GivenThereIsAServiceRunningOn(port, context =>
        {
            if (string.IsNullOrEmpty(basePath))
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else if (context.Request.Path.Value != basePath)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            return Task.CompletedTask;
        });
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
