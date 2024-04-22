using Consul;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using System.Text.RegularExpressions;

namespace Ocelot.AcceptanceTests.ServiceDiscovery
{
    public class ConsulServiceDiscoveryTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly List<ServiceEntry> _consulServices;
        private int _counterOne;
        private int _counterTwo;
        private int _counterConsul;
        private static readonly object SyncLock = new();
        private string _downstreamPath;
        private string _receivedToken;
        private readonly ServiceHandler _serviceHandler;
        private readonly ServiceHandler _serviceHandler2;
        private readonly ServiceHandler _consulHandler;

        public ConsulServiceDiscoveryTests()
        {
            _serviceHandler = new ServiceHandler();
            _serviceHandler2 = new ServiceHandler();
            _consulHandler = new ServiceHandler();
            _steps = new Steps();
            _consulServices = new List<ServiceEntry>();
        }

        [Fact]
        public void should_use_consul_service_discovery_and_load_balance_request()
        {
            var consulPort = PortFinder.GetRandomPort();
            var servicePort1 = PortFinder.GetRandomPort();
            var servicePort2 = PortFinder.GetRandomPort();
            var serviceName = "product";
            var downstreamServiceOneUrl = $"http://localhost:{servicePort1}";
            var downstreamServiceTwoUrl = $"http://localhost:{servicePort2}";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = servicePort1,
                    ID = Guid.NewGuid().ToString(),
                    Tags = Array.Empty<string>(),
                },
            };
            var serviceEntryTwo = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = servicePort2,
                    ID = Guid.NewGuid().ToString(),
                    Tags = Array.Empty<string>(),
                },
            };

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            ServiceName = serviceName,
                            LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                        },
                    },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "http",
                        Host = "localhost",
                        Port = consulPort,
                    },
                },
            };

            this.Given(x => x.GivenProductServiceOneIsRunning(downstreamServiceOneUrl, 200))
                .And(x => x.GivenProductServiceTwoIsRunning(downstreamServiceTwoUrl, 200))
                .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithConsul())
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 50))
                .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(50))
                .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(24, 26))
                .BDDfy();
        }

        [Fact]
        public void should_handle_request_to_consul_for_downstream_service_and_make_request()
        {
            var consulPort = PortFinder.GetRandomPort();
            var servicePort = PortFinder.GetRandomPort();
            const string serviceName = "web";
            var downstreamServiceOneUrl = $"http://localhost:{servicePort}";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = servicePort,
                    ID = "web_90_0_2_224_8080",
                    Tags = new[] { "version-v1" },
                },
            };

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/home",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/home",
                            UpstreamHttpMethod = new List<string> { "Get", "Options" },
                            ServiceName = serviceName,
                            LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                        },
                    },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "http",
                        Host = "localhost",
                        Port = consulPort,
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn(downstreamServiceOneUrl, "/api/home", 200, "Hello from Laura"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunningWithConsul())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/home"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
        }

        [Fact]
        public void should_handle_request_to_consul_for_downstream_service_and_make_request_no_re_routes()
        {
            var consulPort = PortFinder.GetRandomPort();
            const string serviceName = "web";
            var downstreamServicePort = PortFinder.GetRandomPort();
            var downstreamServiceOneUrl = $"http://localhost:{downstreamServicePort}";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = downstreamServicePort,
                    ID = "web_90_0_2_224_8080",
                    Tags = new[] { "version-v1" },
                },
            };

            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "http",
                        Host = "localhost",
                        Port = consulPort,
                    },
                    DownstreamScheme = "http",
                    HttpHandlerOptions = new FileHttpHandlerOptions
                    {
                        AllowAutoRedirect = true,
                        UseCookieContainer = true,
                        UseTracing = false,
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn(downstreamServiceOneUrl, "/something", 200, "Hello from Laura"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunningWithConsul())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/web/something"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
        }

        [Fact]
        public void should_use_consul_service_discovery_and_load_balance_request_no_re_routes()
        {
            var consulPort = PortFinder.GetRandomPort();
            var serviceName = "product";
            var serviceOnePort = PortFinder.GetRandomPort();
            var serviceTwoPort = PortFinder.GetRandomPort();
            var downstreamServiceOneUrl = $"http://localhost:{serviceOnePort}";
            var downstreamServiceTwoUrl = $"http://localhost:{serviceTwoPort}";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = serviceOnePort,
                    ID = Guid.NewGuid().ToString(),
                    Tags = Array.Empty<string>(),
                },
            };
            var serviceEntryTwo = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = serviceTwoPort,
                    ID = Guid.NewGuid().ToString(),
                    Tags = Array.Empty<string>(),
                },
            };

            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "http",
                        Host = "localhost",
                        Port = consulPort,
                    },
                    LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                    DownstreamScheme = "http",
                },
            };

            this.Given(x => x.GivenProductServiceOneIsRunning(downstreamServiceOneUrl, 200))
                .And(x => x.GivenProductServiceTwoIsRunning(downstreamServiceTwoUrl, 200))
                .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithConsul())
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimes($"/{serviceName}/", 50))
                .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(50))
                .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(24, 26))
                .BDDfy();
        }

        [Fact]
        public void should_use_token_to_make_request_to_consul()
        {
            var token = "abctoken";
            var consulPort = PortFinder.GetRandomPort();
            var serviceName = "web";
            var servicePort = PortFinder.GetRandomPort();
            var downstreamServiceOneUrl = $"http://localhost:{servicePort}";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = servicePort,
                    ID = "web_90_0_2_224_8080",
                    Tags = new[] { "version-v1" },
                },
            };

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/home",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/home",
                            UpstreamHttpMethod = new List<string> { "Get", "Options" },
                            ServiceName = serviceName,
                            LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                        },
                    },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "http",
                        Host = "localhost",
                        Port = consulPort,
                        Token = token,
                    },
                },
            };

            this.Given(_ => GivenThereIsAServiceRunningOn(downstreamServiceOneUrl, "/api/home", 200, "Hello from Laura"))
                .And(_ => GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(_ => GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
                .And(_ => _steps.GivenThereIsAConfiguration(configuration))
                .And(_ => _steps.GivenOcelotIsRunningWithConsul())
                .When(_ => _steps.WhenIGetUrlOnTheApiGateway("/home"))
                .Then(_ => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(_ => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(_ => ThenTheTokenIs(token))
                .BDDfy();
        }

        [Fact]
        public void should_send_request_to_service_after_it_becomes_available_in_consul()
        {
            var consulPort = PortFinder.GetRandomPort();
            var serviceName = "product";
            var servicePort1 = PortFinder.GetRandomPort();
            var servicePort2 = PortFinder.GetRandomPort();
            var downstreamServiceOneUrl = $"http://localhost:{servicePort1}";
            var downstreamServiceTwoUrl = $"http://localhost:{servicePort2}";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = servicePort1,
                    ID = Guid.NewGuid().ToString(),
                    Tags = Array.Empty<string>(),
                },
            };
            var serviceEntryTwo = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = servicePort2,
                    ID = Guid.NewGuid().ToString(),
                    Tags = Array.Empty<string>(),
                },
            };

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            ServiceName = serviceName,
                            LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                        },
                    },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "http",
                        Host = "localhost",
                        Port = consulPort,
                    },
                },
            };

            this.Given(x => x.GivenProductServiceOneIsRunning(downstreamServiceOneUrl, 200))
                .And(x => x.GivenProductServiceTwoIsRunning(downstreamServiceTwoUrl, 200))
                .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithConsul())
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

        [Fact]
        public void should_handle_request_to_poll_consul_for_downstream_service_and_make_request()
        {
            var consulPort = PortFinder.GetRandomPort();
            const string serviceName = "web";
            var downstreamServicePort = PortFinder.GetRandomPort();
            var downstreamServiceOneUrl = $"http://localhost:{downstreamServicePort}";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryOne = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceName,
                    Address = "localhost",
                    Port = downstreamServicePort,
                    ID = $"web_90_0_2_224_{downstreamServicePort}",
                    Tags = new[] { "version-v1" },
                },
            };

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/api/home",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/home",
                            UpstreamHttpMethod = new List<string> { "Get", "Options" },
                            ServiceName = serviceName,
                            LoadBalancerOptions = new FileLoadBalancerOptions { Type = "LeastConnection" },
                        },
                    },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "http",
                        Host = "localhost",
                        Port = consulPort,
                        Type = "PollConsul",
                        PollingInterval = 0,
                        Namespace = string.Empty,
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn(downstreamServiceOneUrl, "/api/home", 200, "Hello from Laura"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunningWithConsul())
                .When(x => _steps.WhenIGetUrlOnTheApiGatewayWaitingForTheResponseToBeOk("/home"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
        }

        [Theory]
        [Trait("PR", "1944")]
        [Trait("Issues", "849 1496")]
        [InlineData("LeastConnection")]
        [InlineData("RoundRobin")]
        [InlineData("NoLoadBalancer")]
        [InlineData("CookieStickySessions")]
        public void Should_use_consul_service_discovery_based_on_upstream_host(string loadBalancerType)
        {
            // Simulate two DIFFERENT downstream services (e.g. product services for US and EU markets)
            // with different ServiceNames (e.g. product-us and product-eu),
            // UpstreamHost is used to determine which ServiceName to use when making a request to Consul (e.g. Host: us-shop goes to product-us) 
            var consulPort = PortFinder.GetRandomPort();
            var servicePortUS = PortFinder.GetRandomPort();
            var servicePortEU = PortFinder.GetRandomPort();
            var serviceNameUS = "product-us";
            var serviceNameEU = "product-eu";
            var downstreamServiceUrlUS = $"http://localhost:{servicePortUS}";
            var downstreamServiceUrlEU = $"http://localhost:{servicePortEU}";
            var upstreamHostUS = "us-shop";
            var upstreamHostEU = "eu-shop";
            var publicUrlUS = $"http://{upstreamHostUS}";
            var publicUrlEU = $"http://{upstreamHostEU}";
            var responseBodyUS = "Phone chargers with US plug";
            var responseBodyEU = "Phone chargers with EU plug";
            var fakeConsulServiceDiscoveryUrl = $"http://localhost:{consulPort}";
            var serviceEntryUS = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceNameUS,
                    Address = "localhost",
                    Port = servicePortUS,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[] { "US" },
                },
            };
            var serviceEntryEU = new ServiceEntry
            {
                Service = new AgentService
                {
                    Service = serviceNameEU,
                    Address = "localhost",
                    Port = servicePortEU,
                    ID = Guid.NewGuid().ToString(),
                    Tags = new string[] { "EU" },
                },
            };

            var configuration = new FileConfiguration
            {
                Routes = new()
                {
                    new()
                    {
                        DownstreamPathTemplate = "/products",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new() { "Get" },
                        UpstreamHost = upstreamHostUS,
                        ServiceName = serviceNameUS,
                        LoadBalancerOptions = new() { Type = loadBalancerType },
                    },
                    new()
                    {
                        DownstreamPathTemplate = "/products",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new() {"Get" },
                        UpstreamHost = upstreamHostEU,
                        ServiceName = serviceNameEU,
                        LoadBalancerOptions = new() { Type = loadBalancerType },
                    },
                },
                GlobalConfiguration = new()
                {
                    ServiceDiscoveryProvider = new()
                    {
                        Scheme = "http",
                        Host = "localhost",
                        Port = consulPort,
                    },
                },
            };

            // Ocelot request for http://us-shop/ should find 'product-us' in Consul, call /products and return "Phone chargers with US plug"
            // Ocelot request for http://eu-shop/ should find 'product-eu' in Consul, call /products and return "Phone chargers with EU plug"
            this.Given(x => x._serviceHandler.GivenThereIsAServiceRunningOn(downstreamServiceUrlUS, "/products", MapGet("/products", responseBodyUS)))
                .And(x => x._serviceHandler2.GivenThereIsAServiceRunningOn(downstreamServiceUrlEU, "/products", MapGet("/products", responseBodyEU)))
                .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(fakeConsulServiceDiscoveryUrl))
                .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryUS, serviceEntryEU))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithConsul(publicUrlUS, publicUrlEU))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(publicUrlUS), "When I get US shop for the first time")
                .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(1))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(responseBodyUS))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(publicUrlEU), "When I get EU shop for the first time")
                .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(2))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(responseBodyEU))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(publicUrlUS), "When I get US shop again")
                .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(3))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(responseBodyUS))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway(publicUrlEU), "When I get EU shop again")
                .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(4))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe(responseBodyEU))
                .BDDfy();
        }

        private void ThenTheTokenIs(string token)
        {
            _receivedToken.ShouldBe(token);
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
            _counterConsul = 0;
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
            foreach (var serviceEntry in serviceEntries)
            {
                _consulServices.Add(serviceEntry);
            }
        }

        private void GivenThereIsAFakeConsulServiceDiscoveryProvider(string url)
        {
            _consulHandler.GivenThereIsAServiceRunningOn(url, async context =>
            {
                if (context.Request.Headers.TryGetValue("X-Consul-Token", out var values))
                {
                    _receivedToken = values.First();
                }

                // Parse the request path to get the service name
                var pathMatch = Regex.Match(context.Request.Path.Value, "/v1/health/service/(?<serviceName>[^/]+)");
                if (pathMatch.Success)
                {
                    _counterConsul++;

                    // Use the parsed service name to filter the registered Consul services
                    var serviceName = pathMatch.Groups["serviceName"].Value;
                    var services = _consulServices.Where(x => x.Service.Service == serviceName).ToList();
                    var json = JsonConvert.SerializeObject(services);
                    context.Response.Headers.Append("Content-Type", "application/json");
                    await context.Response.WriteAsync(json);
                }
            });
        }

        private void ThenConsulShouldHaveBeenCalledTimes(int expected)
        {
            _counterConsul.ShouldBe(expected);
        }

        private void GivenProductServiceOneIsRunning(string url, int statusCode)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
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
        }

        private void GivenProductServiceTwoIsRunning(string url, int statusCode)
        {
            _serviceHandler2.GivenThereIsAServiceRunningOn(url, async context =>
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
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
            {
                _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                if (_downstreamPath != basePath)
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
        }

        private RequestDelegate MapGet(string path, string responseBody) => async context =>
        {
            var downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;
            if (downstreamPath == path)
            {
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(responseBody);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Not Found");
            }
        };

        public void Dispose()
        {
            _serviceHandler?.Dispose();
            _serviceHandler2?.Dispose();
            _consulHandler?.Dispose();
            _steps.Dispose();
        }
    }
}
