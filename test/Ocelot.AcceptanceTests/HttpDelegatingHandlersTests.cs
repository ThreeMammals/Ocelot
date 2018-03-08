using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class HttpDelegatingHandlersTests
    {
        private IWebHost _builder;
        private readonly Steps _steps;
        private string _downstreamPath;

        public HttpDelegatingHandlersTests()
        {
            _steps = new Steps();
        }


        [Fact]
        public void should_call_di_handlers()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "localhost",
                                Port = 7187,
                            }
                        },
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    }
                }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:7187", "/", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithHandlersRegisteredInDi<FakeHandler, FakeHandlerTwo>())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => ThenTheHandlersAreCalledCorrectly())
                .BDDfy();
        }


        [Fact]
        public void should_call_di_handlers_with_dependency()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "localhost",
                                Port = 7188,
                            }
                        },
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    }
                }
            };

            var dependency = new FakeDependency();

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:7188", "/", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithHandlersRegisteredInDi<FakeHandlerWithDependency>(dependency))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => ThenTheDependencyIsCalled(dependency))
                .BDDfy();
        }

        private void ThenTheDependencyIsCalled(FakeDependency dependency)
        {
            dependency.Called.ShouldBeTrue();
        }

        private void ThenTheHandlersAreCalledCorrectly()
        {
            FakeHandler.TimeCalled.ShouldBeLessThan(FakeHandlerTwo.TimeCalled);
        }

        public class FakeDependency
        {            
            public bool Called;
        }

        class FakeHandlerWithDependency : DelegatingHandler
        {
            private FakeDependency _dependency;

            public FakeHandlerWithDependency(FakeDependency dependency)
            {
                _dependency = dependency;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _dependency.Called = true;
                return await base.SendAsync(request, cancellationToken);
            }
        }
        
        class FakeHandler : DelegatingHandler
        {  
            public static DateTime TimeCalled { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                TimeCalled = DateTime.Now;
                return await base.SendAsync(request, cancellationToken);
            }
        }
        class FakeHandlerTwo : DelegatingHandler
        {  
            public static DateTime TimeCalled { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                TimeCalled = DateTime.Now;
                return await base.SendAsync(request, cancellationToken);
            }
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody)
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

                        if (_downstreamPath != basePath)
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
    }
}
