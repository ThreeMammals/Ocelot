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
                            UpstreamPathTemplate = "/api/ClientRateLimit",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            RequestIdKey = _steps.RequestIdKey,
                             
                            RateLimitOptions =    new FileRateLimitRule()
                            {
                                EnableRateLimiting = true,
                                ClientWhitelist = new List<string>(),
                                Limit = 3,
                                Period = "1s",
                                PeriodTimespan = 1000
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
                        RateLimitCounterPrefix = "",
                         HttpStatusCode = 428

                    },
                     RequestIdKey ="oceclientrequest"
                }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879/api/ClientRateLimit"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit",1))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(200))
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 2))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(200))
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit",1))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(428))
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
                            UpstreamPathTemplate = "/api/ClientRateLimit",
                            UpstreamHttpMethod = new List<string> { "Get" },
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

  
    }
}