using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Consul;
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
        private IWebHost _builderOne;
        private IWebHost _builderTwo;
        private IWebHost _fakeConsulBuilder;
        private readonly Steps _steps;
        private readonly List<ServiceEntry> _serviceEntries;
        private int _counterOne;
        private int _counterTwo;
        private static readonly object _syncLock = new object();

        public ServiceDiscoveryTests()
        {
            _steps = new Steps();
            _serviceEntries = new List<ServiceEntry>();
        }

        [Fact]
        public void should_use_service_discovery_and_load_balance_request()
        {
            var consulPort = 8502;
            var serviceName = "product";
            var downstreamServiceOneUrl = "http://localhost:50881";
            var downstreamServiceTwoUrl = "http://localhost:50882";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = 50881,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };
            var serviceEntryTwo = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = 50882,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };

            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            ServiceName = serviceName,
                            LoadBalancer = "LeastConnection",
                            UseServiceDiscovery = true,
                        }
                    },
                    GlobalConfiguration = new FileGlobalConfiguration()
                    {
                        ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                        {
                            Host = "localhost",
                            Port = consulPort
                        }
                    }
            };

            this.Given(x => x.GivenProductServiceOneIsRunning(downstreamServiceOneUrl, 200))
                .And(x => x.GivenProductServiceTwoIsRunning(downstreamServiceTwoUrl, 200))
                .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 50))
                .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(50))
                .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(24, 26))
                .BDDfy();
        }

        [Fact]
        public void should_send_request_to_service_after_it_becomes_available()
        {
            var consulPort = 8501;
            var serviceName = "product";
            var downstreamServiceOneUrl = "http://localhost:50879";
            var downstreamServiceTwoUrl = "http://localhost:50880";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = 50879,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };
            var serviceEntryTwo = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = 50880,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };

            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            ServiceName = serviceName,
                            LoadBalancer = "LeastConnection",
                            UseServiceDiscovery = true,
                        }
                    },
                    GlobalConfiguration = new FileGlobalConfiguration()
                    {
                        ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                        {
                            Host = "localhost",
                            Port = consulPort
                        }
                    }
            };

            this.Given(x => x.GivenProductServiceOneIsRunning(downstreamServiceOneUrl, 200))
                .And(x => x.GivenProductServiceTwoIsRunning(downstreamServiceTwoUrl, 200))
                .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 10))
                .And(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(10))
                .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(4, 6))
                .And(x => WhenIRemoveAService(serviceEntryTwo))
                .And(x => GivenIResetCounters())
                .And(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 10))
                .And(x => ThenOnlyOneServiceHasBeenCalled())
                .And(x => WhenIAddAServiceBackIn(serviceEntryTwo))
                .And(x => GivenIResetCounters())
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 10))
                .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(10))
                .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(4, 6))
                .BDDfy();
        }

        private void WhenIAddAServiceBackIn(ServiceEntry serviceEntryTwo)
        {
            _serviceEntries.Add(serviceEntryTwo);
        }

        private void ThenOnlyOneServiceHasBeenCalled()
        {
            _counterOne.ShouldBe(10);
            _counterTwo.ShouldBe(0);
        }

        private void WhenIRemoveAService(ServiceEntry serviceEntryTwo)
        {
            _serviceEntries.Remove(serviceEntryTwo);
        }

         private void GivenIResetCounters()
        {
            _counterOne = 0;
            _counterTwo = 0;
        }

        private void ThenBothServicesCalledRealisticAmountOfTimes(int bottom, int top)
        {
            _counterOne.ShouldBeInRange(bottom, top);
            _counterOne.ShouldBeInRange(bottom, top);
        }

        private void ThenTheTwoServicesShouldHaveBeenCalledTimes(int expected)
        {
            var total = _counterOne + _counterTwo;
            total.ShouldBe(expected);
        }

        private void GivenTheServicesAreRegisteredWithConsul(params ServiceEntry[] serviceEntries)
        {
            foreach(var serviceEntry in serviceEntries)
            {
                _serviceEntries.Add(serviceEntry);
            }
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
                                    if(context.Request.Path.Value == "/v1/health/service/product")
                                    {
                                        await context.Response.WriteJsonAsync(_serviceEntries);
                                    }
                                });
                            })
                            .Build();

            _fakeConsulBuilder.Start();
        }

        private void GivenProductServiceOneIsRunning(string url, int statusCode)
        {
            _builderOne = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        try
                        {
                            var response = string.Empty;
                            lock (_syncLock)
                            {
                                _counterOne++;
                                response = _counterOne.ToString();
                            }
                            context.Response.StatusCode = statusCode;
                            await context.Response.WriteAsync(response);
                        }
                        catch (System.Exception exception)
                        {
                            await context.Response.WriteAsync(exception.StackTrace);
                        }
                    });
                })
                .Build();

            _builderOne.Start();
        }

        private void GivenProductServiceTwoIsRunning(string url, int statusCode)
        {
            _builderTwo = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        try
                        {
                            var response = string.Empty;
                            lock (_syncLock)
                            {
                                _counterTwo++;
                                response = _counterTwo.ToString();
                            }
                            
                            context.Response.StatusCode = statusCode;
                            await context.Response.WriteAsync(response);
                        }
                        catch (System.Exception exception)
                        {
                            await context.Response.WriteAsync(exception.StackTrace);
                        }
                   
                    });
                })
                .Build();

            _builderTwo.Start();
        }

        public void Dispose()
        {
            _builderOne?.Dispose();
            _builderTwo?.Dispose();
            _steps.Dispose();
        }
    }
}
