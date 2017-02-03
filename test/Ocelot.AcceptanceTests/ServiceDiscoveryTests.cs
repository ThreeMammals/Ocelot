using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class ServiceDiscoveryTests : IDisposable
    {
        private IWebHost _builder;
        private IWebHost _fakeConsulBuilder;
        private readonly Steps _steps;

        public ServiceDiscoveryTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_use_service_discovery_and_load_balance_request()
        {
            var serviceName = "product";
            var downstreamServiceOneUrl = "http://localhost:51879";
            var downstreamServiceTwoUrl = "http://localhost:51880";
            var fakeConsulServiceDiscoveryUrl = "http://localhost:9500";
            var downstreamServiceOneCounter = 0;
            var downstreamServiceTwoCounter = 0;

            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get",
                            ServiceName = serviceName,
                            LoadBalancer = "LeastConnection",
                        }
                    },
                    GlobalConfiguration = new FileGlobalConfiguration()
                    {
                        ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                        {
                            Provider = "Consul"
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn(downstreamServiceOneUrl, 200, downstreamServiceOneCounter))
                .And(x => x.GivenThereIsAServiceRunningOn(downstreamServiceTwoUrl, 200, downstreamServiceTwoCounter))
                .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceName, downstreamServiceOneUrl, downstreamServiceTwoUrl))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 50))
                .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(50, downstreamServiceOneCounter, downstreamServiceTwoCounter))
                .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(downstreamServiceOneCounter,downstreamServiceTwoCounter))
                .BDDfy();
        }

        private void ThenBothServicesCalledRealisticAmountOfTimes(int counterOne, int counterTwo)
        {
            counterOne.ShouldBeGreaterThan(10);
            counterTwo.ShouldBeGreaterThan(10);
        }

        private void ThenTheTwoServicesShouldHaveBeenCalledTimes(int expected, int counterOne, int counterTwo)
        {
            var total = counterOne + counterTwo;
            total.ShouldBe(expected);
        }

        private void GivenTheServicesAreRegisteredWithConsul(string serviceName, params string[] urls)
        {
            //register these services with fake consul
        }

        private void GivenThereIsAFakeConsulServiceDiscoveryProvider(string url)
        {
            _fakeConsulBuilder = new WebHostBuilder()
                            .UseUrls(url)
                            .UseKestrel()
                            .UseContentRoot(Directory.GetCurrentDirectory())
                            .UseIISIntegration()
                            .UseUrls(url)
                            .Configure(app =>
                            {
                                app.Run(async context =>
                                {
                                    //do consul shit
                                });
                            })
                            .Build();

            _fakeConsulBuilder.Start();
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode, int counter)
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
                        counter++;
                        context.Response.StatusCode = statusCode;
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
