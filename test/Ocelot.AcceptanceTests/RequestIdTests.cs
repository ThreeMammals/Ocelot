using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration.File;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class RequestIdTests : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;

        public RequestIdTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_use_default_request_id_and_forward()
        {
            var configuration = new FileConfiguration
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
                                    Port = 51879,
                                }
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            RequestIdKey = _steps.RequestIdKey,
                         }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheRequestIdIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_use_request_id_and_forward()
        {
            var configuration = new FileConfiguration
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
                                    Port = 51879,
                                }
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    }
            };

            var requestId = Guid.NewGuid().ToString();

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/", requestId))
                .Then(x => _steps.ThenTheRequestIdIsReturned(requestId))
                .BDDfy();
        }

        [Fact]
        public void should_use_global_request_id_and_forward()
        {
            var configuration = new FileConfiguration
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
                                    Port = 51879,
                                }
                            },
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                        }
                    },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = _steps.RequestIdKey
                }
            };

            var requestId = Guid.NewGuid().ToString();

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/", requestId))
                .Then(x => _steps.ThenTheRequestIdIsReturned(requestId))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string url)
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
                        StringValues requestId;
                        context.Request.Headers.TryGetValue(_steps.RequestIdKey, out requestId);
                        context.Response.Headers.Add(_steps.RequestIdKey, requestId.First());
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
