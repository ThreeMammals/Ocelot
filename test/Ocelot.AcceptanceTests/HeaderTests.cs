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
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class HeaderTests : IDisposable
    {
        private IWebHost _builder;
        private int _count;
        private readonly Steps _steps;

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
                                    Port = 51871,
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

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51871", "/", 200, "Laz"))
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
                                    Port = 51871,
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

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51871", "/", 200, "Location", "http://www.bbc.co.uk/"))
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

        [Fact]
        public void request_should_reuse_cookies_with_cookie_container()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/sso/{everything}",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "localhost",
                                Port = 6774,
                            }
                        },
                        UpstreamPathTemplate = "/sso/{everything}",
                        UpstreamHttpMethod = new List<string> { "Get", "Post", "Options" },
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            UseCookieContainer = true
                        }
                    }
                }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:6774", "/sso/test", 200))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/sso/test"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseHeaderIs("Set-Cookie", "test=0; path=/"))
                .And(x => _steps.GivenIAddCookieToMyRequest("test=1; path=/"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/sso/test"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }
        
        [Fact]
        public void request_should_have_own_cookies_no_cookie_container()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/sso/{everything}",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "localhost",
                                Port = 6775,
                            }
                        },
                        UpstreamPathTemplate = "/sso/{everything}",
                        UpstreamHttpMethod = new List<string> { "Get", "Post", "Options" },
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            UseCookieContainer = false
                        }
                    }
                }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:6775", "/sso/test", 200))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/sso/test"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseHeaderIs("Set-Cookie", "test=0; path=/"))
                .And(x => _steps.GivenIAddCookieToMyRequest("test=1; path=/"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/sso/test"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }
        
        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode)
        {
            _builder = new WebHostBuilder()
                .UseUrls(baseUrl)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .Configure(app =>
                {
                    app.UsePathBase(basePath);
                    app.Run(context =>
                    {
                        if (_count == 0)
                        {
                            context.Response.Cookies.Append("test", "0");
                            _count++;
                            context.Response.StatusCode = statusCode;
                            return Task.CompletedTask;
                        }

                        if (context.Request.Cookies.TryGetValue("test", out var cookieValue) || context.Request.Headers.TryGetValue("Set-Cookie", out var headerValue))
                        {
                            if (cookieValue == "0" || headerValue == "test=1; path=/")
                            {
                                context.Response.StatusCode = statusCode;
                                return Task.CompletedTask;
                            }
                        }

                        context.Response.StatusCode = 500;
                        return Task.CompletedTask;
                    });
                })
                .Build();

            _builder.Start();
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
                    app.Run(context => 
                    {   
                        context.Response.OnStarting(() => {
                            context.Response.Headers.Add(headerKey, headerValue);
                            context.Response.StatusCode = statusCode;
                            return Task.CompletedTask;
                        });

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
