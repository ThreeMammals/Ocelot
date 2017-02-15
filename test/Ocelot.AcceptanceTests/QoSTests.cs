using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class QoSTests : IDisposable
    {
        private IWebHost _brokenService;
        private readonly Steps _steps;
        private int _requestCount;
        private IWebHost _workingService;

        public QoSTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_open_circuit_breaker_then_close()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "http",
                        DownstreamHost = "localhost",
                        DownstreamPort = 51879,
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = "Get",
                        QoSOptions = new FileQoSOptions
                        {
                            ExceptionsAllowedBeforeBreaking = 1,
                            TimeoutValue = 500,
                            DurationOfBreak = 1000
                        }
                    }
                }
            };

            this.Given(x => x.GivenThereIsAPossiblyBrokenServiceRunningOn("http://localhost:51879", "Hello from Laura"))
                .Given(x => _steps.GivenThereIsAConfiguration(configuration))
                .Given(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .Given(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .Given(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .Given(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .Given(x => x.GivenIWaitMilliseconds(3000))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void open_circuit_should_not_effect_different_reRoute()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "http",
                        DownstreamHost = "localhost",
                        DownstreamPort = 51879,
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = "Get",
                        QoSOptions = new FileQoSOptions
                        {
                            ExceptionsAllowedBeforeBreaking = 1,
                            TimeoutValue = 500,
                            DurationOfBreak = 1000
                        }
                    },
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "http",
                        DownstreamHost = "localhost",
                        DownstreamPort = 51880,
                        UpstreamPathTemplate = "working",
                        UpstreamHttpMethod = "Get",
                    }
                }
            };

            this.Given(x => x.GivenThereIsAPossiblyBrokenServiceRunningOn("http://localhost:51879", "Hello from Laura"))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51880/", 200, "Hello from Tom"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/working"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Tom"))
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .And(x => x.GivenIWaitMilliseconds(3000))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        private void GivenIWaitMilliseconds(int ms)
        {
            Thread.Sleep(ms);
        }

        private void GivenThereIsAPossiblyBrokenServiceRunningOn(string url, string responseBody)
        {
            _brokenService = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        //circuit starts closed
                        if (_requestCount == 0)
                        {
                            _requestCount++;
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync(responseBody);
                            return;
                        }

                        //request one times out and polly throws exception, circuit opens
                        if (_requestCount == 1)
                        {
                            _requestCount++;
                            await Task.Delay(1000);
                            context.Response.StatusCode = 200;
                            return;
                        }

                        //after break closes we return 200 OK
                        if (_requestCount == 2)
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync(responseBody);
                            return;
                        }
                    });
                })
                .Build();

            _brokenService.Start();
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode, string responseBody)
        {
            _workingService = new WebHostBuilder()
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
                        await context.Response.WriteAsync(responseBody);
                    });
                })
                .Build();

            _workingService.Start();
        }

        public void Dispose()
        {
            _workingService?.Dispose();
            _brokenService?.Dispose();
            _steps.Dispose();
        }
    }
}
