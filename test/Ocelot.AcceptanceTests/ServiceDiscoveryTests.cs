namespace Ocelot.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using Consul;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Configuration.File;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;
    using Newtonsoft.Json;
    using Pivotal.Discovery.Client;

    public class ServiceDiscoveryTests : IDisposable
    {
        private IWebHost _builderOne;
        private IWebHost _builderTwo;
        private IWebHost _fakeConsulBuilder;
        private readonly Steps _steps;
        private readonly List<ServiceEntry> _consulServices;
        private readonly List<IServiceInstance> _eurekaInstances;
        private int _counterOne;
        private int _counterTwo;
        private static readonly object SyncLock = new object();
        private IWebHost _builder;
        private string _downstreamPath;
        private string _receivedToken;

        public ServiceDiscoveryTests()
        {
            _steps = new Steps();
            _consulServices = new List<ServiceEntry>();
            _eurekaInstances = new List<IServiceInstance>();
        }

        [Fact]
        public void should_use_eureka_service_discovery_and_make_request()
        {
            var eurekaPort = 8761;
            var serviceName = "product";
            var downstreamServicePort = 50371;
            var downstreamServiceOneUrl = $"http://localhost:{downstreamServicePort}";
            var fakeEurekaServiceDiscoveryUrl = $"http://localhost:{eurekaPort}";

            var instanceOne = new FakeEurekaService(serviceName, "localhost", downstreamServicePort, false,
                new Uri($"http://localhost:{downstreamServicePort}"), new Dictionary<string, string>());
       
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
                            LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                            UseServiceDiscovery = true,
                        }
                    },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Type = "Eureka"
                    }
                }
            };

            this.Given(x => x.GivenEurekaProductServiceOneIsRunning(downstreamServiceOneUrl, 200))
                .And(x => x.GivenThereIsAFakeEurekaServiceDiscoveryProvider(fakeEurekaServiceDiscoveryUrl, serviceName))
                .And(x => x.GivenTheServicesAreRegisteredWithEureka(instanceOne))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))                
                .And(_ => _steps.ThenTheResponseBodyShouldBe(nameof(ServiceDiscoveryTests)))
                .BDDfy();
        }

        [Fact]
        public void should_use_consul_service_discovery_and_load_balance_request()
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
                            LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
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
                .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl, serviceName))
                .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 50))
                .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(50))
                .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(24, 26))
                .BDDfy();
        }

        [Fact]
        public void should_handle_request_to_consul_for_downstream_service_and_make_request()
        {
            const int consulPort = 8505;
            const string serviceName = "web";
            const string downstreamServiceOneUrl = "http://localhost:8080";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = 8080,
                    ID = "web_90_0_2_224_8080",
                    Tags = new[] {"version-v1"}
                },
            };

            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/home",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/home",
                            UpstreamHttpMethod = new List<string> { "Get", "Options" },
                            ServiceName = serviceName,
                            LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
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

            this.Given(x => x.GivenThereIsAServiceRunningOn(downstreamServiceOneUrl, "/api/home", 200, "Hello from Laura"))                
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl, serviceName))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/home"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
        }

        [Fact]
        public void should_handle_request_to_consul_for_downstream_service_and_make_request_no_re_routes()
        {
            const int consulPort = 8513;
            const string serviceName = "web";
            const int downstreamServicePort = 8087;
            var downstreamServiceOneUrl = $"http://localhost:{downstreamServicePort}";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = downstreamServicePort,
                    ID = "web_90_0_2_224_8080",
                    Tags = new[] {"version-v1"}
                },
            };

            var configuration = new FileConfiguration
            {
                    GlobalConfiguration = new FileGlobalConfiguration
                    {
                        ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                        {
                            Host = "localhost",
                            Port = consulPort
                        },
                        DownstreamScheme = "http",
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            AllowAutoRedirect = true,
                            UseCookieContainer = true,
                            UseTracing = false
                        },
                        QoSOptions = new FileQoSOptions
                        {
                            TimeoutValue = 100,
                            DurationOfBreak = 1000,
                            ExceptionsAllowedBeforeBreaking = 1
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn(downstreamServiceOneUrl, "/something", 200, "Hello from Laura"))                
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl, serviceName))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/web/something"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
        }

        [Fact]
        public void should_use_consul_service_discovery_and_load_balance_request_no_re_routes()
        {
            var consulPort = 8510;
            var serviceName = "product";
            var serviceOnePort = 50888;
            var serviceTwoPort = 50889;
            var downstreamServiceOneUrl = $"http://localhost:{serviceOnePort}";
            var downstreamServiceTwoUrl = $"http://localhost:{serviceTwoPort}";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = serviceOnePort,
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
                    Port = serviceTwoPort,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[0]
                },
            };

            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = consulPort
                    },
                    LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                    DownstreamScheme = "http"
                }
            };

            this.Given(x => x.GivenProductServiceOneIsRunning(downstreamServiceOneUrl, 200))
                .And(x => x.GivenProductServiceTwoIsRunning(downstreamServiceTwoUrl, 200))
                .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl, serviceName))
                .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimes($"/{serviceName}/", 50))
                .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(50))
                .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(24, 26))
                .BDDfy();
        }

        [Fact]
        public void should_use_token_to_make_request_to_consul()
        {
            var token = "abctoken";
            var consulPort = 8515;
            var serviceName = "web";
            var downstreamServiceOneUrl = "http://localhost:8081";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = 8081,
                    ID = "web_90_0_2_224_8080",
                    Tags = new[] { "version-v1" }
                },
            };

            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/home",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/home",
                            UpstreamHttpMethod = new List<string> { "Get", "Options" },
                            ServiceName = serviceName,
                            LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                            UseServiceDiscovery = true,
                        }
                    },
                GlobalConfiguration = new FileGlobalConfiguration()
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider()
                    {
                        Host = "localhost",
                        Port = consulPort,
                        Token = token
                    }
                }
            };

            this.Given(_ => GivenThereIsAServiceRunningOn(downstreamServiceOneUrl, "/api/home", 200, "Hello from Laura"))
                .And(_ => GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl, serviceName))
                .And(_ => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
                .And(_ => _steps.GivenThereIsAConfiguration(configuration))
                .And(_ => _steps.GivenOcelotIsRunning())
                .When(_ => _steps.WhenIGetUrlOnTheApiGateway("/home"))
                .Then(_ => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(_ => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(_ => _receivedToken.ShouldBe(token))
                .BDDfy();
        }

        [Fact]
        public void should_send_request_to_service_after_it_becomes_available_in_consul()
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
                            LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
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
                .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl, serviceName))
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
            _consulServices.Add(serviceEntryTwo);
        }

        private void ThenOnlyOneServiceHasBeenCalled()
        {
            _counterOne.ShouldBe(10);
            _counterTwo.ShouldBe(0);
        }

        private void WhenIRemoveAService(ServiceEntry serviceEntryTwo)
        {
            _consulServices.Remove(serviceEntryTwo);
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
                _consulServices.Add(serviceEntry);
            }
        }

        private void GivenTheServicesAreRegisteredWithEureka(params IServiceInstance[] serviceInstances)
        {
            foreach (var instance in serviceInstances)
            {
                _eurekaInstances.Add(instance);
            }
        }

        private void GivenThereIsAFakeEurekaServiceDiscoveryProvider(string url, string serviceName)
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
                        if (context.Request.Path.Value == "/eureka/apps/")
                        {
                            var apps = new List<Application>();

                            foreach (var serviceInstance in _eurekaInstances)
                            {
                                var a = new Application
                                {
                                    name = serviceName,
                                    instance = new List<Instance>
                                    {
                                        new Instance
                                        {
                                            instanceId = $"{serviceInstance.Host}:{serviceInstance}",
                                            hostName = serviceInstance.Host,
                                            app = serviceName,
                                            ipAddr = "127.0.0.1",
                                            status = "UP",
                                            overriddenstatus = "UNKNOWN",
                                            port = new Port {value = serviceInstance.Port, enabled = "true"},
                                            securePort = new SecurePort {value = serviceInstance.Port, enabled = "true"},
                                            countryId = 1,
                                            dataCenterInfo = new DataCenterInfo {value = "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", name = "MyOwn"},
                                            leaseInfo = new LeaseInfo
                                            {
                                                renewalIntervalInSecs = 30,
                                                durationInSecs = 90,
                                                registrationTimestamp = 1457714988223,
                                                lastRenewalTimestamp= 1457716158319,
                                                evictionTimestamp = 0,
                                                serviceUpTimestamp = 1457714988223
                                            },
                                            metadata = new Metadata
                                            {
                                                value = "java.util.Collections$EmptyMap"
                                            },
                                            homePageUrl = $"{serviceInstance.Host}:{serviceInstance.Port}",
                                            statusPageUrl = $"{serviceInstance.Host}:{serviceInstance.Port}",
                                            healthCheckUrl = $"{serviceInstance.Host}:{serviceInstance.Port}",
                                            vipAddress = serviceName,
                                            isCoordinatingDiscoveryServer = "false",
                                            lastUpdatedTimestamp = "1457714988223",
                                            lastDirtyTimestamp = "1457714988172",
                                            actionType = "ADDED"
                                        }
                                    }
                                };

                                apps.Add(a);
                            }

                            var applications = new EurekaApplications
                            {
                                applications = new Applications
                                {
                                    application = apps,
                                    apps__hashcode = "UP_1_",
                                    versions__delta = "1"
                                }
                            };

                            await context.Response.WriteJsonAsync(applications);
                        }
                    });
                })
                .Build();

            _fakeConsulBuilder.Start();
        }

        private void GivenThereIsAFakeConsulServiceDiscoveryProvider(string url, string serviceName)
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
                                    if(context.Request.Path.Value == $"/v1/health/service/{serviceName}")
                                    {
                                        if (context.Request.Headers.TryGetValue("X-Consul-Token", out var values))
                                        {
                                            _receivedToken = values.First();
                                        }

                                        await context.Response.WriteJsonAsync(_consulServices);
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
                            string response;
                            lock (SyncLock)
                            {
                                _counterOne++;
                                response = _counterOne.ToString();
                            }
                            context.Response.StatusCode = statusCode;
                            await context.Response.WriteAsync(response);
                        }
                        catch (Exception exception)
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
                            string response;
                            lock (SyncLock)
                            {
                                _counterTwo++;
                                response = _counterTwo.ToString();
                            }
                            
                            context.Response.StatusCode = statusCode;
                            await context.Response.WriteAsync(response);
                        }
                        catch (Exception exception)
                        {
                            await context.Response.WriteAsync(exception.StackTrace);
                        }                  
                    });
                })
                .Build();

            _builderTwo.Start();
        }

        private void GivenEurekaProductServiceOneIsRunning(string url, int statusCode)
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
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync(nameof(ServiceDiscoveryTests));
                        }
                        catch (Exception exception)
                        {
                            await context.Response.WriteAsync(exception.StackTrace);
                        }
                    });
                })
                .Build();

            _builderOne.Start();
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

                        if(_downstreamPath != basePath)
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

        public void Dispose()
        {
            _builderOne?.Dispose();
            _builderTwo?.Dispose();
            _steps.Dispose();
        }
    }

    public class FakeEurekaService : IServiceInstance
    {
        public FakeEurekaService(string serviceId, string host, int port, bool isSecure, Uri uri, IDictionary<string, string> metadata)
        {
            ServiceId = serviceId;
            Host = host;
            Port = port;
            IsSecure = isSecure;
            Uri = uri;
            Metadata = metadata;
        }

        public string ServiceId { get; }
        public string Host { get; }
        public int Port { get; }
        public bool IsSecure { get; }
        public Uri Uri { get; }
        public IDictionary<string, string> Metadata { get; }
    }

    public class Port
    {
        [JsonProperty("$")]
        public int value { get; set; }

        [JsonProperty("@enabled")]
        public string enabled { get; set; }
    }

    public class SecurePort
    {
        [JsonProperty("$")]
        public int value { get; set; }

        [JsonProperty("@enabled")]
        public string enabled { get; set; }
    }

    public class DataCenterInfo
    {
        [JsonProperty("@class")]
        public string value { get; set; }

        public string name { get; set; }
    }

    public class LeaseInfo
    {
        public int renewalIntervalInSecs { get; set; }

        public int durationInSecs { get; set; }

        public long registrationTimestamp { get; set; }

        public long lastRenewalTimestamp { get; set; }

        public int evictionTimestamp { get; set; }

        public long serviceUpTimestamp { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("@class")]
        public string value { get; set; }
    }

    public class Instance
    {
        public string instanceId { get; set; }
        public string hostName { get; set; }
        public string app { get; set; }
        public string ipAddr { get; set; }
        public string status { get; set; }
        public string overriddenstatus { get; set; }
        public Port port { get; set; }
        public SecurePort securePort { get; set; }
        public int countryId { get; set; }
        public DataCenterInfo dataCenterInfo { get; set; }
        public LeaseInfo leaseInfo { get; set; }
        public Metadata metadata { get; set; }
        public string homePageUrl { get; set; }
        public string statusPageUrl { get; set; }
        public string healthCheckUrl { get; set; }
        public string vipAddress { get; set; }
        public string isCoordinatingDiscoveryServer { get; set; }
        public string lastUpdatedTimestamp { get; set; }
        public string lastDirtyTimestamp { get; set; }
        public string actionType { get; set; }
    }

    public class Application
    {
        public string name { get; set; }
        public List<Instance> instance { get; set; }
    }

    public class Applications
    {
        public string versions__delta { get; set; }
        public string apps__hashcode { get; set; }
        public List<Application> application { get; set; }
    }

    public class EurekaApplications
    {
        public Applications applications { get; set; }
    }
}
