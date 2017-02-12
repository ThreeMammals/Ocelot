using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class ClientRateLimitTests : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;
         private int _counterOne;


        public ClientRateLimitTests()
        {
            _steps = new Steps();
        }


        public void Dispose()
        {
            _builder?.Dispose();
            _steps.Dispose();
        }

        [Fact]
        public void should_call_withratelimiting()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/ClientRateLimit",
                            DownstreamPort = 51879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamTemplate = "/api/ClientRateLimit",
                            UpstreamHttpMethod = "Get",
                            RequestIdKey = _steps.RequestIdKey,
                             
                            RateLimitOptions =    new FileRateLimitRule()
                            {
                                EnableRateLimiting = true,
                                ClientWhitelist = new List<string>(),
                                Limit = 3,
                                Period = "1s",
                                PeriodTimespan = 100
                            }
                        }
                },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    RateLimitOptions = new FileRateLimitOptions()
                    {
                        ClientIdHeader = "ClientId",
                        DisableRateLimitHeaders = false,
                        QuotaExceededMessage = "",
                        RateLimitCounterPrefix = ""                         
                    },
                     RequestIdKey ="oceclientrequest"
                }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879/api/ClientRateLimit"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 5))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(429))
                .BDDfy();
        }


        [Fact]
        public void should_call_middleware_withWhitelistClient()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/ClientRateLimit",
                            DownstreamPort = 51879,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamTemplate = "/api/ClientRateLimit",
                            UpstreamHttpMethod = "Get",
                            RequestIdKey = _steps.RequestIdKey,

                            RateLimitOptions =    new FileRateLimitRule()
                            {
                                EnableRateLimiting = true,
                                ClientWhitelist = new List<string>() { "ocelotclient1"},
                                Limit = 3,
                                Period = "1s",
                                PeriodTimespan = 100
                            }
                        }
                },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    RateLimitOptions = new FileRateLimitOptions()
                    {
                        ClientIdHeader = "ClientId",
                        DisableRateLimitHeaders = false,
                        QuotaExceededMessage = "",
                        RateLimitCounterPrefix = ""
                    },
                    RequestIdKey = "oceclientrequest"
                }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879/api/ClientRateLimit"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 4))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(200))
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
                        _counterOne++;
                        context.Response.StatusCode = 200;
                        context.Response.WriteAsync(_counterOne.ToString());
                        return Task.CompletedTask;
                    });
                })
                .Build();

            _builder.Start();
        }

        //private void GetApiRateLimait(string url)
        //{
        //    var clientId = "ocelotclient1";
        //     var request = new HttpRequestMessage(new HttpMethod("GET"), url);
        //        request.Headers.Add("ClientId", clientId);

        //        var response = _client.SendAsync(request);
        //        responseStatusCode = (int)response.Result.StatusCode;
        //    }

        //}

        //public void WhenIGetUrlOnTheApiGatewayMultipleTimes(string url, int times)
        //{
        //    var clientId = "ocelotclient1";
        //    var tasks = new Task[times];

        //    for (int i = 0; i < times; i++)
        //    {
        //        var urlCopy = url;
        //        tasks[i] = GetForServiceDiscoveryTest(urlCopy);
        //        Thread.Sleep(_random.Next(40, 60));
        //    }

        //    Task.WaitAll(tasks);
        //}

        //private void WhenICallTheMiddlewareWithWhiteClient()
        //{
        //    var clientId = "ocelotclient2";
        //    // Act    
        //    for (int i = 0; i < 2; i++)
        //    {
        //        var request = new HttpRequestMessage(new HttpMethod("GET"), apiRateLimitPath);
        //        request.Headers.Add("ClientId", clientId);

        //        var response = _client.SendAsync(request);
        //        responseStatusCode = (int)response.Result.StatusCode;
        //    }
        //}

        //private void ThenresponseStatusCodeIs429()
        //{
        //    responseStatusCode.ShouldBe(429);
        //}

        //private void ThenresponseStatusCodeIs200()
        //{
        //    responseStatusCode.ShouldBe(200);
        //}
    }
}