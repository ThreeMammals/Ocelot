using Consul;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Infrastructure;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

public sealed class ConsulServiceDiscoveryTests : Steps, IDisposable
{
    private readonly List<ServiceEntry> _consulServices;
    private readonly List<Node> _consulNodes;
    private int _counterOne;
    private int _counterTwo;
    private int _counterConsul;
    private int _counterNodes;
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
        _consulServices = new();
        _consulNodes = new();
    }

    public override void Dispose()
    {
        _serviceHandler?.Dispose();
        _serviceHandler2?.Dispose();
        _consulHandler?.Dispose();
    }

    [Fact]
    public void Should_use_consul_service_discovery_and_load_balance_request()
    {
        const string serviceName = "product";
        var consulPort = PortFinder.GetRandomPort();
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var serviceEntryOne = GivenServiceEntry(port1, serviceName: serviceName);
        var serviceEntryTwo = GivenServiceEntry(port2, serviceName: serviceName);
        var route = GivenRoute(serviceName: serviceName);
        var configuration = GivenServiceDiscovery(consulPort, route);
        this.Given(x => x.GivenProductServiceOneIsRunning(DownstreamUrl(port1), 200))
            .And(x => x.GivenProductServiceTwoIsRunning(DownstreamUrl(port2), 200))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne, serviceEntryTwo))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithConsul())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 50))
            .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(50))
            .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(24, 26))
            .BDDfy();
    }

    [Fact]
    public void Should_handle_request_to_consul_for_downstream_service_and_make_request()
    {
        const string serviceName = "web";
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();
        var serviceEntryOne = GivenServiceEntry(servicePort, "localhost", "web_90_0_2_224_8080", new[] { "version-v1" }, serviceName);
        var route = GivenRoute("/api/home", "/home", serviceName, httpMethods: new[] { "Get", "Options" });
        var configuration = GivenServiceDiscovery(consulPort, route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(servicePort), "/api/home", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithConsul())
            .When(x => WhenIGetUrlOnTheApiGateway("/home"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_handle_request_to_consul_for_downstream_service_and_make_request_no_re_routes()
    {
        const string serviceName = "web";
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();
        var serviceEntry = GivenServiceEntry(servicePort, "localhost", "web_90_0_2_224_8080", new[] { "version-v1" }, serviceName);

        var configuration = GivenServiceDiscovery(consulPort);
        configuration.GlobalConfiguration.DownstreamScheme = "http";
        configuration.GlobalConfiguration.HttpHandlerOptions = new()
        {
            AllowAutoRedirect = true,
            UseCookieContainer = true,
            UseTracing = false,
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(servicePort), "/something", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntry))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithConsul())
            .When(x => WhenIGetUrlOnTheApiGateway("/web/something"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_use_consul_service_discovery_and_load_balance_request_no_re_routes()
    {
        const string serviceName = "product";
        var consulPort = PortFinder.GetRandomPort();
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var serviceEntry1 = GivenServiceEntry(port1, serviceName: serviceName);
        var serviceEntry2 = GivenServiceEntry(port2, serviceName: serviceName);

        var configuration = GivenServiceDiscovery(consulPort);
        configuration.GlobalConfiguration.LoadBalancerOptions = new() { Type = nameof(LeastConnection) };
        configuration.GlobalConfiguration.DownstreamScheme = "http";

        this.Given(x => x.GivenProductServiceOneIsRunning(DownstreamUrl(port1), 200))
            .And(x => x.GivenProductServiceTwoIsRunning(DownstreamUrl(port2), 200))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntry1, serviceEntry2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithConsul())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes($"/{serviceName}/", 50))
            .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(50))
            .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(24, 26))
            .BDDfy();
    }

    [Fact]
    public void Should_use_token_to_make_request_to_consul()
    {
        const string serviceName = "web";
        const string token = "abctoken";
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();
        var serviceEntry = GivenServiceEntry(servicePort, "localhost", "web_90_0_2_224_8080", new[] { "version-v1" }, serviceName);
        var route = GivenRoute("/api/home", "/home", serviceName, httpMethods: new[] { "Get", "Options" });

        var configuration = GivenServiceDiscovery(consulPort, route);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider.Token = token;

        this.Given(_ => GivenThereIsAServiceRunningOn(DownstreamUrl(servicePort), "/api/home", HttpStatusCode.OK, "Hello from Laura"))
            .And(_ => GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(_ => GivenTheServicesAreRegisteredWithConsul(serviceEntry))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunningWithConsul())
            .When(_ => WhenIGetUrlOnTheApiGateway("/home"))
            .Then(_ => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(_ => ThenTheTokenIs(token))
            .BDDfy();
    }

    [Fact]
    public void Should_send_request_to_service_after_it_becomes_available_in_consul()
    {
        const string serviceName = "product";
        var consulPort = PortFinder.GetRandomPort();
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var serviceEntry1 = GivenServiceEntry(port1, serviceName: serviceName);
        var serviceEntry2 = GivenServiceEntry(port2, serviceName: serviceName);
        var route = GivenRoute(serviceName: serviceName);
        var configuration = GivenServiceDiscovery(consulPort, route);
        this.Given(x => x.GivenProductServiceOneIsRunning(DownstreamUrl(port1), 200))
            .And(x => x.GivenProductServiceTwoIsRunning(DownstreamUrl(port2), 200))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntry1, serviceEntry2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithConsul())
            .And(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 10))
            .And(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(10))
            .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(4, 6))
            .And(x => WhenIRemoveAService(serviceEntry2))
            .And(x => GivenIResetCounters())
            .And(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 10))
            .And(x => ThenOnlyOneServiceHasBeenCalled())
            .And(x => WhenIAddAServiceBackIn(serviceEntry2))
            .And(x => GivenIResetCounters())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 10))
            .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(10))
            .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(4, 6))
            .BDDfy();
    }

    [Fact]
    public void Should_handle_request_to_poll_consul_for_downstream_service_and_make_request()
    {
        const string serviceName = "web";
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();
        var serviceEntry = GivenServiceEntry(servicePort, "localhost", $"web_90_0_2_224_{servicePort}", new[] { "version-v1" }, serviceName);
        var route = GivenRoute("/api/home", "/home", serviceName, httpMethods: new[] { "Get", "Options" });
        var configuration = GivenServiceDiscovery(consulPort, route);

        var sd = configuration.GlobalConfiguration.ServiceDiscoveryProvider;
        sd.Type = nameof(PollConsul);
        sd.PollingInterval = 0;
        sd.Namespace = string.Empty;

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(servicePort), "/api/home", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntry))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithConsul())
            .When(x => WhenIGetUrlOnTheApiGatewayWaitingForTheResponseToBeOk("/home"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Theory]
    [Trait("PR", "1944")]
    [Trait("Bugs", "849 1496")]
    [InlineData(nameof(LeastConnection))]
    [InlineData(nameof(RoundRobin))]
    [InlineData(nameof(NoLoadBalancer))]
    [InlineData(nameof(CookieStickySessions))]
    public void Should_use_consul_service_discovery_based_on_upstream_host(string loadBalancerType)
    {
        // Simulate two DIFFERENT downstream services (e.g. product services for US and EU markets)
        // with different ServiceNames (e.g. product-us and product-eu),
        // UpstreamHost is used to determine which ServiceName to use when making a request to Consul (e.g. Host: us-shop goes to product-us) 
        const string serviceNameUS = "product-us";
        const string serviceNameEU = "product-eu";
        var consulPort = PortFinder.GetRandomPort();
        var servicePortUS = PortFinder.GetRandomPort();
        var servicePortEU = PortFinder.GetRandomPort();
        const string upstreamHostUS = "us-shop";
        const string upstreamHostEU = "eu-shop";
        var publicUrlUS = $"http://{upstreamHostUS}";
        var publicUrlEU = $"http://{upstreamHostEU}";
        const string responseBodyUS = "Phone chargers with US plug";
        const string responseBodyEU = "Phone chargers with EU plug";
        var serviceEntryUS = GivenServiceEntry(servicePortUS, serviceName: serviceNameUS, tags: new[] { "US" });
        var serviceEntryEU = GivenServiceEntry(servicePortEU, serviceName: serviceNameEU, tags: new[] { "EU" });
        var routeUS = GivenRoute("/products", "/", serviceNameUS, loadBalancerType, upstreamHostUS);
        var routeEU = GivenRoute("/products", "/", serviceNameEU, loadBalancerType, upstreamHostEU);
        var configuration = GivenServiceDiscovery(consulPort, routeUS, routeEU);

        // Ocelot request for http://us-shop/ should find 'product-us' in Consul, call /products and return "Phone chargers with US plug"
        // Ocelot request for http://eu-shop/ should find 'product-eu' in Consul, call /products and return "Phone chargers with EU plug"
        this.Given(x => x._serviceHandler.GivenThereIsAServiceRunningOn(DownstreamUrl(servicePortUS), "/products", MapGet("/products", responseBodyUS)))
            .And(x => x._serviceHandler2.GivenThereIsAServiceRunningOn(DownstreamUrl(servicePortEU), "/products", MapGet("/products", responseBodyEU)))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryUS, serviceEntryEU))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithConsul(publicUrlUS, publicUrlEU))
            .When(x => WhenIGetUrlOnTheApiGateway(publicUrlUS), "When I get US shop for the first time")
            .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(1))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(responseBodyUS))
            .When(x => WhenIGetUrlOnTheApiGateway(publicUrlEU), "When I get EU shop for the first time")
            .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(2))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(responseBodyEU))
            .When(x => WhenIGetUrlOnTheApiGateway(publicUrlUS), "When I get US shop again")
            .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(3))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(responseBodyUS))
            .When(x => WhenIGetUrlOnTheApiGateway(publicUrlEU), "When I get EU shop again")
            .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(4))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(responseBodyEU))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "954")]
    public void Should_return_service_address_by_overridden_service_builder_when_there_is_a_node()
    {
        const string serviceName = "OpenTestService";
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort(); // 9999
        var serviceEntry = GivenServiceEntry(servicePort,
            id: "OPEN_TEST_01",
            serviceName: serviceName,
            tags: new[] { serviceName });
        var serviceNode = new Node() { Name = "n1" }; // cornerstone of the bug
        serviceEntry.Node = serviceNode;
        var route = GivenRoute("/api/{url}", "/open/{url}", serviceName, httpMethods: new[] { "POST", "GET" });
        var configuration = GivenServiceDiscovery(consulPort, route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(servicePort), "/api/home", HttpStatusCode.OK, "Hello from Raman"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntry))
            .And(x => x.GivenTheServiceNodesAreRegisteredWithConsul(serviceNode))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithConsul()) // default services registration results with the bug: "n1" host issue
            .When(x => WhenIGetUrlOnTheApiGateway("/open/home"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
            .And(x => ThenTheResponseBodyShouldBe(""))
            .And(x => ThenConsulShouldHaveBeenCalledTimes(1))
            .And(x => ThenConsulNodesShouldHaveBeenCalledTimes(1))

            // Override default service builder
            .Given(x => GivenOcelotIsRunningWithServices(WithOverriddenConsulServiceBuilder))
            .When(x => WhenIGetUrlOnTheApiGateway("/open/home"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Raman"))
            .And(x => ThenConsulShouldHaveBeenCalledTimes(2))
            .And(x => ThenConsulNodesShouldHaveBeenCalledTimes(2))
            .BDDfy();
    }

    private static void WithOverriddenConsulServiceBuilder(IServiceCollection services)
        => services.AddOcelot().AddConsul<MyConsulServiceBuilder>();

    public class MyConsulServiceBuilder : DefaultConsulServiceBuilder
    {
        public MyConsulServiceBuilder(Func<ConsulRegistryConfiguration> configurationFactory, IConsulClientFactory clientFactory, IOcelotLoggerFactory loggerFactory)
            : base(configurationFactory, clientFactory, loggerFactory) { }

        protected override string GetDownstreamHost(ServiceEntry entry, Node node) => entry.Service.Address;
    }

    private static ServiceEntry GivenServiceEntry(int port, string address = null, string id = null, string[] tags = null, [CallerMemberName] string serviceName = null) => new()
    {
        Service = new AgentService
        {
            Service = serviceName,
            Address = address ?? "localhost",
            Port = port,
            ID = id ?? Guid.NewGuid().ToString(),
            Tags = tags ?? Array.Empty<string>(),
        },
    };

    private static FileRoute GivenRoute(string downstream = null, string upstream = null, [CallerMemberName] string serviceName = null, string loadBalancerType = null, string upstreamHost = null, string[] httpMethods = null) => new()
    {
        DownstreamPathTemplate = downstream ?? "/",
        DownstreamScheme = Uri.UriSchemeHttp,
        DownstreamHostAndPorts = new List<FileHostAndPort>()
        {
            new FileHostAndPort("localhost",5000)
        },
        UpstreamPathTemplate = upstream ?? "/",
        UpstreamHttpMethod = httpMethods != null ? new(httpMethods) : new() { HttpMethods.Get },
        UpstreamHost = upstreamHost,
        ServiceName = serviceName,
        LoadBalancerOptions = new() { Type = loadBalancerType ?? nameof(LeastConnection) },
    };

    private static FileConfiguration GivenServiceDiscovery(int consulPort, params FileRoute[] routes)
    {
        var config = GivenConfiguration(routes);
        config.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = Uri.UriSchemeHttp,
            Host = "localhost",
            Port = consulPort,
            Type = nameof(Provider.Consul.Consul),
        };
        return config;
    }

    private void ThenTheTokenIs(string token)
    {
        _receivedToken.ShouldBe(token);
    }

    private void WhenIAddAServiceBackIn(ServiceEntry serviceEntry)
    {
        _consulServices.Add(serviceEntry);
    }

    private void ThenOnlyOneServiceHasBeenCalled()
    {
        _counterOne.ShouldBe(10);
        _counterTwo.ShouldBe(0);
    }

    private void WhenIRemoveAService(ServiceEntry serviceEntry)
    {
        _consulServices.Remove(serviceEntry);
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

    private void GivenTheServicesAreRegisteredWithConsul(params ServiceEntry[] serviceEntries) => _consulServices.AddRange(serviceEntries);
    private void GivenTheServiceNodesAreRegisteredWithConsul(params Node[] nodes) => _consulNodes.AddRange(nodes);

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
                var json = JsonSerializer.Serialize(services, JsonSerializerOptionsExtensions.Web);
                json = json.Replace("\"Name\":", "\"Node\":");
                context.Response.Headers.Append("Content-Type", "application/json");
                await context.Response.WriteAsync(json);
                return;
            }

            if (context.Request.Path.Value == "/v1/catalog/nodes")
            {
                _counterNodes++;
                var json = JsonSerializer.Serialize(_consulNodes, JsonSerializerOptionsExtensions.Web);
                context.Response.Headers.Append("Content-Type", "application/json");
                await context.Response.WriteAsync(json);
            }
        });
    }

    private void ThenConsulShouldHaveBeenCalledTimes(int expected) => _counterConsul.ShouldBe(expected);
    private void ThenConsulNodesShouldHaveBeenCalledTimes(int expected) => _counterNodes.ShouldBe(expected);

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

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
        {
            _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

            if (_downstreamPath != basePath)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync("Downstream path doesn't match base path");
            }
            else
            {
                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsync(responseBody);
            }
        });
    }

    private static RequestDelegate MapGet(string path, string responseBody) => async context =>
    {
        var downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;
        if (downstreamPath == path)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(responseBody);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await context.Response.WriteAsync("Not Found");
        }
    };
}
