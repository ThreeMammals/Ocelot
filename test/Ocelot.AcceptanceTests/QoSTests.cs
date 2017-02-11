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
        private IWebHost _builder;
        private readonly Steps _steps;
        private int _requestCount;

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

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879", "Hello from Laura"))
                .Given(x => _steps.GivenThereIsAConfiguration(configuration))
                .Given(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .Given(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.RequestTimeout))
                .Given(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.RequestTimeout))
                .Given(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.RequestTimeout))
                .Given(x => x.GivenIWaitMilliSeconds(2000))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        private void GivenIWaitMilliSeconds(int ms)
        {
            Thread.Sleep(ms);
        }

        private void GivenThereIsAServiceRunningOn(string url, string responseBody)
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
                        //circuit starts closed
                        if (_requestCount == 0)
                        {
                            _requestCount++;
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync(responseBody);
                            return;
                        }

                        //request one times out and polly throws exception
                        if (_requestCount == 1)
                        {
                            _requestCount++;
                            await Task.Delay(1000);
                            context.Response.StatusCode = 200;
                            return;
                        }

                        //request two times out and polly throws exception circuit opens
                        if (_requestCount == 2)
                        {
                            _requestCount++;
                            await Task.Delay(1000);
                            context.Response.StatusCode = 200;
                            return;
                        }

                        //after break closes we return 200 OK
                        if (_requestCount == 3)
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync(responseBody);
                            return;
                        }
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
