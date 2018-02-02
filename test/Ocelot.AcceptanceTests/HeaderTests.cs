using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
    public class HeaderTests : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;
        private string _downstreamPath;

        public HeaderTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_transform_upstream_header()
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
                                    Port = 51879,
                                }
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            UpstreamHeaderTransform = new Dictionary<string,string>
                            {
                                {"Laz", "D, GP"}
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879", "/", 200, "Laz"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenIAddAHeader("Laz", "D"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("GP"))
                .BDDfy();
        }

        [Fact]
        public void should_transform_downstream_header()
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
                                    Port = 51879,
                                }
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            DownstreamHeaderTransform = new Dictionary<string,string>
                            {
                                {"Location", "http://www.bbc.co.uk/, http://ocelot.com/"}
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879", "/", 200, "Location", "http://www.bbc.co.uk/"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseHeaderIs("Location", "http://ocelot.com/"))
                .BDDfy();
        }

        [Fact]
        public void should_fix_issue_190()
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
                                    Port = 6773,
                                }
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            DownstreamHeaderTransform = new Dictionary<string,string>
                            {
                                {"Location", "http://localhost:6773, {BaseUrl}"}
                            },
                            HttpHandlerOptions = new FileHttpHandlerOptions
                            {
                                AllowAutoRedirect = false
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:6773", "/", 302, "Location", "http://localhost:6773/pay/Receive"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Redirect))
                .And(x => _steps.ThenTheResponseHeaderIs("Location", "http://localhost:5000/pay/Receive"))
                .BDDfy();
        }

        [Fact]
        public void should_fix_issue_205()
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
                                    Port = 6773,
                                }
                            },
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            DownstreamHeaderTransform = new Dictionary<string,string>
                            {
                                {"Location", "{DownstreamBaseUrl}, {BaseUrl}"}
                            },
                            HttpHandlerOptions = new FileHttpHandlerOptions
                            {
                                AllowAutoRedirect = false
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:6773", "/", 302, "Location", "http://localhost:6773/pay/Receive"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Redirect))
                .And(x => _steps.ThenTheResponseHeaderIs("Location", "http://localhost:5000/pay/Receive"))
                .BDDfy();
        }


        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string headerKey)
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
                        if(context.Request.Headers.TryGetValue(headerKey, out var values))
                        {
                            var result = values.First();
                            context.Response.StatusCode = statusCode;
                            await context.Response.WriteAsync(result);
                        }   
                    });
                })
                .Build();

            _builder.Start();
        }

         private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string headerKey, string headerValue)
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
                        context.Response.OnStarting(() => {
                            context.Response.Headers.Add(headerKey, headerValue);
                            context.Response.StatusCode = statusCode;
                            return Task.CompletedTask;
                        });
                    });
                })
                .Build();

            _builder.Start();
        }

        internal void ThenTheDownstreamUrlPathShouldBe(string expectedDownstreamPath)
        {
            _downstreamPath.ShouldBe(expectedDownstreamPath);
        }

        public void Dispose()
        {
            _builder?.Dispose();
            _steps.Dispose();
        }
    }
}
