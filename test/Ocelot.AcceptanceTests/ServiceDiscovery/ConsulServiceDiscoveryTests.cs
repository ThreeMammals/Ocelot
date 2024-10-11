using Consul;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Ocelot.AcceptanceTests.LoadBalancer;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.ServiceDiscovery.Providers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

/// <summary>
/// Tests for the <see cref="Provider.Consul.Consul"/> provider.
/// </summary>
public sealed partial class ConsulServiceDiscoveryTests : ConcurrentSteps, IDisposable
{
    private readonly ServiceHandler _consulHandler;
    private readonly List<ServiceEntry> _consulServices;
    private readonly List<Node> _consulNodes;

    private string _receivedToken;

    private volatile int _counterConsul;
    private volatile int _counterNodes;

    public ConsulServiceDiscoveryTests()
    {
        _consulHandler = new ServiceHandler();
        _consulServices = new List<ServiceEntry>();
        _consulNodes = new List<Node>();
    }

    public override void Dispose()
    {
        _consulHandler?.Dispose();
        base.Dispose();
    }

    [Fact]
    [Trait("Feat", "28")]
    public void ShouldDiscoverServicesInConsulAndLoadBalanceByLeastConnectionWhenConfigInRoute()
    {
        const string serviceName = "product";
        var consulPort = PortFinder.GetRandomPort();
        var ports = PortFinder.GetPorts(2);
        var serviceEntries = ports.Select(port => GivenServiceEntry(port, serviceName: serviceName)).ToArray();
        var route = GivenRoute(serviceName: serviceName, loadBalancerType: nameof(LeastConnection));
        var configuration = GivenServiceDiscovery(consulPort, route);
        var urls = ports.Select(DownstreamUrl).ToArray();
        this.Given(x => GivenMultipleServiceInstancesAreRunning(urls, serviceName))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntries))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(WithConsul))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 50))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(50))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(/*25*/24, /*25*/26)) // TODO Check strict assertion
            .BDDfy();
    }

    private static readonly string[] VersionV1Tags = new[] { "version-v1" };
    private static readonly string[] GetVsOptionsMethods = new[] { "Get", "Options" };

    [Fact]
    [Trait("Feat", "201")]
    [Trait("Bug", "213")]
    public void ShouldHandleRequestToConsulForDownstreamServiceAndMakeRequest()
    {
        const string serviceName = "web";
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();
        var serviceEntryOne = GivenServiceEntry(servicePort, "localhost", "web_90_0_2_224_8080", VersionV1Tags, serviceName);
        var route = GivenRoute("/api/home", "/home", serviceName, httpMethods: GetVsOptionsMethods);
        var configuration = GivenServiceDiscovery(consulPort, route);
        this.Given(x => GivenThereIsAServiceRunningOn(DownstreamUrl(servicePort), "/api/home", "Hello from Laura"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryOne))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(WithConsul))
            .When(x => WhenIGetUrlOnTheApiGateway("/home"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "213")]
    [Trait("Feat", "201 340")]
    public void ShouldHandleRequestToConsulForDownstreamServiceAndMakeRequestWhenDynamicRoutingWithNoRoutes()
    {
        const string serviceName = "web";
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();
        var serviceEntry = GivenServiceEntry(servicePort, "localhost", "web_90_0_2_224_8080", VersionV1Tags, serviceName);

        var configuration = GivenServiceDiscovery(consulPort);
        configuration.GlobalConfiguration.DownstreamScheme = "http";
        configuration.GlobalConfiguration.HttpHandlerOptions = new()
        {
            AllowAutoRedirect = true,
            UseCookieContainer = true,
            UseTracing = false,
        };

        this.Given(x => GivenThereIsAServiceRunningOn(DownstreamUrl(servicePort), "/something", "Hello from Laura"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntry))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(WithConsul))
            .When(x => WhenIGetUrlOnTheApiGateway("/web/something"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "340")]
    public void ShouldUseConsulServiceDiscoveryAndLoadBalanceRequestWhenDynamicRoutingWithNoRoutes()
    {
        const string serviceName = "product";
        var consulPort = PortFinder.GetRandomPort();
        var ports = PortFinder.GetPorts(2);
        var serviceEntries = ports.Select(port => GivenServiceEntry(port, serviceName: serviceName)).ToArray();

        var configuration = GivenServiceDiscovery(consulPort);
        configuration.GlobalConfiguration.LoadBalancerOptions = new() { Type = nameof(LeastConnection) };
        configuration.GlobalConfiguration.DownstreamScheme = "http";

        var urls = ports.Select(DownstreamUrl).ToArray();
        this.Given(x => GivenMultipleServiceInstancesAreRunning(urls, serviceName))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntries))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(WithConsul))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently($"/{serviceName}/", 50))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(50))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(/*25*/24, /*25*/26)) // TODO Check strict assertion
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "295")]
    public void ShouldUseAclTokenToMakeRequestToConsul()
    {
        const string serviceName = "web";
        const string token = "abctoken";
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();
        var serviceEntry = GivenServiceEntry(servicePort, "localhost", "web_90_0_2_224_8080", VersionV1Tags, serviceName);
        var route = GivenRoute("/api/home", "/home", serviceName, httpMethods: GetVsOptionsMethods);

        var configuration = GivenServiceDiscovery(consulPort, route);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider.Token = token;

        this.Given(x => GivenThereIsAServiceRunningOn(DownstreamUrl(servicePort), "/api/home", "Hello from Laura"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntry))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(WithConsul))
            .When(x => WhenIGetUrlOnTheApiGateway("/home"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => x.ThenTheTokenIs(token))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "181")]
    public void ShouldSendRequestToServiceAfterItBecomesAvailableInConsul()
    {
        const string serviceName = "product";
        var consulPort = PortFinder.GetRandomPort();
        var ports = PortFinder.GetPorts(2);
        var serviceEntries = ports.Select(port => GivenServiceEntry(port, serviceName: serviceName)).ToArray();
        var route = GivenRoute(serviceName: serviceName);
        var configuration = GivenServiceDiscovery(consulPort, route);
        var urls = ports.Select(DownstreamUrl).ToArray();
        this.Given(_ => GivenMultipleServiceInstancesAreRunning(urls, serviceName))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntries))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(WithConsul))
            .And(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 10))
            .And(x => ThenAllServicesShouldHaveBeenCalledTimes(10))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(/*5*/4, /*5*/6)) // TODO Check strict assertion
            .And(x => x.WhenIRemoveAService(serviceEntries[1])) // 2nd entry
            .And(x => x.GivenIResetCounters())
            .And(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 10))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(10, 0)) // 2nd is offline
            .And(x => x.WhenIAddAServiceBackIn(serviceEntries[1])) // 2nd entry
            .And(x => x.GivenIResetCounters())
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 10))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(10))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(/*5*/4, /*5*/6)) // TODO Check strict assertion
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "374")]
    public void ShouldPollConsulForDownstreamServiceAndMakeRequest()
    {
        const string serviceName = "web";
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort();
        var serviceEntry = GivenServiceEntry(servicePort, "localhost", $"web_90_0_2_224_{servicePort}", VersionV1Tags, serviceName);
        var route = GivenRoute("/api/home", "/home", serviceName, httpMethods: GetVsOptionsMethods);
        var configuration = GivenServiceDiscovery(consulPort, route);

        var sd = configuration.GlobalConfiguration.ServiceDiscoveryProvider;
        sd.Type = nameof(PollConsul); // !!!
        sd.PollingInterval = 0;
        sd.Namespace = string.Empty;

        this.Given(x => GivenThereIsAServiceRunningOn(DownstreamUrl(servicePort), "/api/home", "Hello from Laura"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntry))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(WithConsul))
            .When(x => WhenIGetUrlOnTheApiGatewayWaitingForTheResponseToBeOk("/home"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Theory]
    [Trait("PR", "1944")]
    [Trait("Bugs", "849 1496")]
    [InlineData(nameof(NoLoadBalancer))]
    [InlineData(nameof(RoundRobin))]
    [InlineData(nameof(LeastConnection))]
    [InlineData(nameof(CookieStickySessions))]
    public void ShouldUseConsulServiceDiscoveryWhenThereAreTwoUpstreamHosts(string loadBalancerType)
    {
        // Simulate two DIFFERENT downstream services (e.g. product services for US and EU markets)
        // with different ServiceNames (e.g. product-us and product-eu),
        // UpstreamHost is used to determine which ServiceName to use when making a request to Consul (e.g. Host: us-shop goes to product-us) 
        const string serviceNameUS = "product-us";
        const string serviceNameEU = "product-eu";
        string[] tagsUS = new[] { "US" }, tagsEU = new[] { "EU" };
        var consulPort = PortFinder.GetRandomPort();
        var servicePortUS = PortFinder.GetRandomPort();
        var servicePortEU = PortFinder.GetRandomPort();
        const string upstreamHostUS = "us-shop";
        const string upstreamHostEU = "eu-shop";
        var publicUrlUS = $"http://{upstreamHostUS}";
        var publicUrlEU = $"http://{upstreamHostEU}";
        const string responseBodyUS = "Phone chargers with US plug";
        const string responseBodyEU = "Phone chargers with EU plug";
        var serviceEntryUS = GivenServiceEntry(servicePortUS, serviceName: serviceNameUS, tags: tagsUS);
        var serviceEntryEU = GivenServiceEntry(servicePortEU, serviceName: serviceNameEU, tags: tagsEU);
        var routeUS = GivenRoute("/products", "/", serviceNameUS, loadBalancerType, upstreamHostUS);
        var routeEU = GivenRoute("/products", "/", serviceNameEU, loadBalancerType, upstreamHostEU);
        var configuration = GivenServiceDiscovery(consulPort, routeUS, routeEU);
        bool isStickySession = loadBalancerType == nameof(CookieStickySessions);
        var sessionCookieUS = isStickySession ? new CookieHeaderValue(routeUS.LoadBalancerOptions.Key, Guid.NewGuid().ToString()) : null;
        var sessionCookieEU = isStickySession ? new CookieHeaderValue(routeEU.LoadBalancerOptions.Key, Guid.NewGuid().ToString()) : null;

        // Ocelot request for http://us-shop/ should find 'product-us' in Consul, call /products and return "Phone chargers with US plug"
        // Ocelot request for http://eu-shop/ should find 'product-eu' in Consul, call /products and return "Phone chargers with EU plug"
        _handlers = new ServiceHandler[2] { new(), new() };
        this.Given(x => _handlers[0].GivenThereIsAServiceRunningOn(DownstreamUrl(servicePortUS), "/products", MapGet("/products", responseBodyUS)))
            .Given(x => _handlers[1].GivenThereIsAServiceRunningOn(DownstreamUrl(servicePortEU), "/products", MapGet("/products", responseBodyEU)))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntryUS, serviceEntryEU))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(WithConsul))
            .When(x => x.WhenIGetUrl(publicUrlUS, sessionCookieUS), "When I get US shop for the first time")
            .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(1))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(responseBodyUS))
            .When(x => x.WhenIGetUrl(publicUrlEU, sessionCookieEU), "When I get EU shop for the first time")
            .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(2))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(responseBodyEU))
            .When(x => x.WhenIGetUrl(publicUrlUS, sessionCookieUS), "When I get US shop again")
            .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(isStickySession ? 2 : 3)) // sticky sessions use cache, so Consul shouldn't be called
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(responseBodyUS))
            .When(x => x.WhenIGetUrl(publicUrlEU, sessionCookieEU), "When I get EU shop again")
            .Then(x => x.ThenConsulShouldHaveBeenCalledTimes(isStickySession ? 2 : 4)) // sticky sessions use cache, so Consul shouldn't be called
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(responseBodyEU))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "954")]
    public void ShouldReturnServiceAddressByOverriddenServiceBuilderWhenThereIsANode()
    {
        const string serviceName = "OpenTestService";
        string[] methods = new[] { HttpMethods.Post, HttpMethods.Get };
        var consulPort = PortFinder.GetRandomPort();
        var servicePort = PortFinder.GetRandomPort(); // 9999
        var serviceEntry = GivenServiceEntry(servicePort,
            id: "OPEN_TEST_01",
            serviceName: serviceName,
            tags: new[] { serviceName });
        var serviceNode = new Node() { Name = "n1" }; // cornerstone of the bug
        serviceEntry.Node = serviceNode;
        var route = GivenRoute("/api/{url}", "/open/{url}", serviceName, httpMethods: methods);
        var configuration = GivenServiceDiscovery(consulPort, route);

        this.Given(x => GivenThereIsAServiceRunningOn(DownstreamUrl(servicePort), "/api/home", "Hello from Raman"))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(serviceEntry))
            .And(x => x.GivenTheServiceNodesAreRegisteredWithConsul(serviceNode))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(WithConsul)) // default services registration results with the bug: "n1" host issue
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

    private static readonly string[] Bug2119ServiceNames = new string[] { "ProjectsService", "CustomersService" };
    private readonly ILoadBalancer[] _lbAnalyzers = new ILoadBalancer[Bug2119ServiceNames.Length]; // emulate LoadBalancerHouse's collection

    private TLoadBalancer GetAnalyzer<TLoadBalancer, TLoadBalancerCreator>(DownstreamRoute route, IServiceDiscoveryProvider provider)
        where TLoadBalancer : class, ILoadBalancer
        where TLoadBalancerCreator : class, ILoadBalancerCreator, new()
    {
        //lock (LoadBalancerHouse.SyncRoot) // Note, synch locking is implemented in LoadBalancerHouse
        int index = Array.IndexOf(Bug2119ServiceNames, route.ServiceName); // LoadBalancerHouse should return different balancers for different service names
        _lbAnalyzers[index] ??= new TLoadBalancerCreator().Create(route, provider)?.Data;
        return (TLoadBalancer)_lbAnalyzers[index];
    }

    private void WithLbAnalyzer<TLoadBalancer, TLoadBalancerCreator>(IServiceCollection services)
        where TLoadBalancer : class, ILoadBalancer
        where TLoadBalancerCreator : class, ILoadBalancerCreator, new()
        => services.AddOcelot().AddConsul().AddCustomLoadBalancer(GetAnalyzer<TLoadBalancer, TLoadBalancerCreator>);

    [Theory]
    [Trait("Bug", "2119")]
    [InlineData(nameof(NoLoadBalancer))]
    [InlineData(nameof(RoundRobin))]
    [InlineData(nameof(LeastConnection))] // original scenario
    public void ShouldReturnDifferentServicesWhenThereAre2SequentialRequestsToDifferentServices(string loadBalancer)
    {
        var consulPort = PortFinder.GetRandomPort();
        var ports = PortFinder.GetPorts(Bug2119ServiceNames.Length);
        var service1 = GivenServiceEntry(ports[0], serviceName: Bug2119ServiceNames[0]);
        var service2 = GivenServiceEntry(ports[1], serviceName: Bug2119ServiceNames[1]);
        var route1 = GivenRoute("/{all}", "/projects/{all}", serviceName: Bug2119ServiceNames[0], loadBalancerType: loadBalancer);
        var route2 = GivenRoute("/{all}", "/customers/{all}", serviceName: Bug2119ServiceNames[1], loadBalancerType: loadBalancer);
        route1.UpstreamHttpMethod = route2.UpstreamHttpMethod = new() { HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete };
        var configuration = GivenServiceDiscovery(consulPort, route1, route2);
        var urls = ports.Select(DownstreamUrl).ToArray();
        this.Given(x => GivenMultipleServiceInstancesAreRunning(urls, Bug2119ServiceNames))
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(service1, service2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(WithConsul))

            // Step 1
            .When(x => WhenIGetUrlOnTheApiGateway("/projects/api/projects"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenServiceShouldHaveBeenCalledTimes(0, 1))
            .And(x => x.ThenTheResponseBodyShouldBe($"1:{Bug2119ServiceNames[0]}")) // !

            // Step 2
            .When(x => WhenIGetUrlOnTheApiGateway("/customers/api/customers"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenServiceShouldHaveBeenCalledTimes(1, 1))
            .And(x => x.ThenTheResponseBodyShouldBe($"1:{Bug2119ServiceNames[1]}")) // !!

            // Finally
            .Then(x => ThenAllStatusCodesShouldBe(HttpStatusCode.OK))
            .And(x => ThenAllServicesShouldHaveBeenCalledTimes(2))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(1, 1))
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "2119")]
    [InlineData(false, nameof(NoLoadBalancer))]
    [InlineData(false, nameof(LeastConnection))] // original scenario, clean config
    [InlineData(true, nameof(LeastConnectionAnalyzer))] // extended scenario using analyzer
    [InlineData(false, nameof(RoundRobin))]
    [InlineData(true, nameof(RoundRobinAnalyzer))]
    public void ShouldReturnDifferentServicesWhenSequentiallylyRequestingToDifferentServices(bool withAnalyzer, string loadBalancer)
    {
        var consulPort = PortFinder.GetRandomPort();
        var ports = PortFinder.GetPorts(Bug2119ServiceNames.Length);
        var service1 = GivenServiceEntry(ports[0], serviceName: Bug2119ServiceNames[0]);
        var service2 = GivenServiceEntry(ports[1], serviceName: Bug2119ServiceNames[1]);
        var route1 = GivenRoute("/{all}", "/projects/{all}", serviceName: Bug2119ServiceNames[0], loadBalancerType: loadBalancer);
        var route2 = GivenRoute("/{all}", "/customers/{all}", serviceName: Bug2119ServiceNames[1], loadBalancerType: loadBalancer);
        route1.UpstreamHttpMethod = route2.UpstreamHttpMethod = new() { HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete };
        var configuration = GivenServiceDiscovery(consulPort, route1, route2);
        var urls = ports.Select(DownstreamUrl).ToArray();
        Func<int, Task> requestToProjectsAndThenRequestToCustomersAndAssert = async (i) =>
        {
            // Step 1
            int count = i + 1;
            await WhenIGetUrlOnTheApiGateway("/projects/api/projects");
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
            ThenServiceShouldHaveBeenCalledTimes(0, count);
            ThenTheResponseBodyShouldBe($"{count}:{Bug2119ServiceNames[0]}", $"i is {i}");
            _responses[2 * i] = _response;

            // Step 2
            await WhenIGetUrlOnTheApiGateway("/customers/api/customers");
            ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
            ThenServiceShouldHaveBeenCalledTimes(1, count);
            ThenTheResponseBodyShouldBe($"{count}:{Bug2119ServiceNames[1]}", $"i is {i}");
            _responses[(2 * i) + 1] = _response;
        };
        this.Given(x => GivenMultipleServiceInstancesAreRunning(urls, Bug2119ServiceNames)) // service names as responses
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(service1, service2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(withAnalyzer ? WithLbAnalyzer(loadBalancer) : WithConsul))
            .When(x => WhenIDoActionMultipleTimes(50, requestToProjectsAndThenRequestToCustomersAndAssert))
            .Then(x => ThenAllStatusCodesShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenResponsesShouldHaveBodyFromDifferentServices(ports, Bug2119ServiceNames)) // !!!
            .And(x => ThenAllServicesShouldHaveBeenCalledTimes(100))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(50, 50))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(50, 50)) // strict assertion
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "2119")]
    [InlineData(false, nameof(NoLoadBalancer))]
    [InlineData(false, nameof(LeastConnection))] // original scenario, clean config
    [InlineData(true, nameof(LeastConnectionAnalyzer))] // extended scenario using analyzer
    [InlineData(false, nameof(RoundRobin))]
    [InlineData(true, nameof(RoundRobinAnalyzer))]
    public void ShouldReturnDifferentServicesWhenConcurrentlyRequestingToDifferentServices(bool withAnalyzer, string loadBalancer)
    {
        const int total = 100; // concurrent requests
        var consulPort = PortFinder.GetRandomPort();
        var ports = PortFinder.GetPorts(Bug2119ServiceNames.Length);
        var service1 = GivenServiceEntry(ports[0], serviceName: Bug2119ServiceNames[0]);
        var service2 = GivenServiceEntry(ports[1], serviceName: Bug2119ServiceNames[1]);
        var route1 = GivenRoute("/{all}", "/projects/{all}", serviceName: Bug2119ServiceNames[0], loadBalancerType: loadBalancer);
        var route2 = GivenRoute("/{all}", "/customers/{all}", serviceName: Bug2119ServiceNames[1], loadBalancerType: loadBalancer);
        route1.UpstreamHttpMethod = route2.UpstreamHttpMethod = new() { HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete };
        var configuration = GivenServiceDiscovery(consulPort, route1, route2);
        var urls = ports.Select(DownstreamUrl).ToArray();
        this.Given(x => GivenMultipleServiceInstancesAreRunning(urls, Bug2119ServiceNames)) // service names as responses
            .And(x => x.GivenThereIsAFakeConsulServiceDiscoveryProvider(DownstreamUrl(consulPort)))
            .And(x => x.GivenTheServicesAreRegisteredWithConsul(service1, service2))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(withAnalyzer ? WithLbAnalyzer(loadBalancer) : WithConsul))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently(total, "/projects/api/projects", "/customers/api/customers"))
            .Then(x => ThenAllStatusCodesShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenResponsesShouldHaveBodyFromDifferentServices(ports, Bug2119ServiceNames)) // !!!
            .And(x => ThenAllServicesShouldHaveBeenCalledTimes(total))
            .And(x => ThenServiceCountersShouldMatchLeasingCounters((ILoadBalancerAnalyzer)_lbAnalyzers[0], ports, 50)) // ProjectsService
            .And(x => ThenServiceCountersShouldMatchLeasingCounters((ILoadBalancerAnalyzer)_lbAnalyzers[1], ports, 50)) // CustomersService
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(Bottom(total, ports.Length), Top(total, ports.Length)))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(50, 50)) // strict assertion
            .BDDfy();
    }

    private Action<IServiceCollection> WithLbAnalyzer(string loadBalancer) => loadBalancer switch
    {
        nameof(LeastConnection) => WithLbAnalyzer<LeastConnection, LeastConnectionCreator>,
        nameof(LeastConnectionAnalyzer) => WithLbAnalyzer<LeastConnectionAnalyzer, LeastConnectionAnalyzerCreator>,
        nameof(RoundRobin) => WithLbAnalyzer<RoundRobin, RoundRobinCreator>,
        nameof(RoundRobinAnalyzer) => WithLbAnalyzer<RoundRobinAnalyzer, RoundRobinAnalyzerCreator>,
        _ => WithLbAnalyzer<LeastConnection, LeastConnectionCreator>,
    };

    private void ThenResponsesShouldHaveBodyFromDifferentServices(int[] ports, string[] serviceNames)
    {
        foreach (var response in _responses)
        {
            var headers = response.Value.Headers;
            headers.TryGetValues(HeaderNames.ServiceIndex, out var indexValues).ShouldBeTrue();
            int serviceIndex = int.Parse(indexValues.FirstOrDefault() ?? "-1");
            serviceIndex.ShouldBeGreaterThanOrEqualTo(0);

            headers.TryGetValues(HeaderNames.Host, out var hostValues).ShouldBeTrue();
            hostValues.FirstOrDefault().ShouldBe("localhost");
            headers.TryGetValues(HeaderNames.Port, out var portValues).ShouldBeTrue();
            portValues.FirstOrDefault().ShouldBe(ports[serviceIndex].ToString());

            var body = response.Value.Content.ReadAsStringAsync().Result;
            var serviceName = serviceNames[serviceIndex];
            body.ShouldNotBeNull().ShouldEndWith(serviceName);

            headers.TryGetValues(HeaderNames.Counter, out var counterValues).ShouldBeTrue();
            var counter = counterValues.ShouldNotBeNull().FirstOrDefault().ShouldNotBeNull();
            body.ShouldBe($"{counter}:{serviceName}");
        }
    }

    private static void WithConsul(IServiceCollection services) => services
        .AddOcelot().AddConsul();

    private static void WithOverriddenConsulServiceBuilder(IServiceCollection services) => services
        .AddOcelot().AddConsul<MyConsulServiceBuilder>();

    public class MyConsulServiceBuilder : DefaultConsulServiceBuilder
    {
        public MyConsulServiceBuilder(IHttpContextAccessor contextAccessor, IConsulClientFactory clientFactory, IOcelotLoggerFactory loggerFactory)
            : base(contextAccessor, clientFactory, loggerFactory) { }

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

    private FileRoute GivenRoute(string downstream = null, string upstream = null, [CallerMemberName] string serviceName = null, string loadBalancerType = null, string upstreamHost = null, string[] httpMethods = null) => new()
    {
        DownstreamPathTemplate = downstream ?? "/",
        DownstreamScheme = Uri.UriSchemeHttp,
        UpstreamPathTemplate = upstream ?? "/",
        UpstreamHttpMethod = httpMethods != null ? new(httpMethods) : new() { HttpMethods.Get },
        UpstreamHost = upstreamHost,
        ServiceName = serviceName,
        LoadBalancerOptions = new()
        {
            Type = loadBalancerType ?? nameof(LeastConnection),
            Key = serviceName,
            Expiry = 60_000,
        },
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

    private async Task WhenIGetUrl(string url, CookieHeaderValue cookie)
    {
        var t = cookie != null
            ? WhenIGetUrlOnTheApiGateway(url, cookie)
            : WhenIGetUrl(url);
        _response = await t;
    }

    private void ThenTheTokenIs(string token)
    {
        _receivedToken.ShouldBe(token);
    }

    private void WhenIAddAServiceBackIn(ServiceEntry serviceEntry)
    {
        _consulServices.Add(serviceEntry);
    }

    private void WhenIRemoveAService(ServiceEntry serviceEntry)
    {
        _consulServices.Remove(serviceEntry);
    }

    private void GivenIResetCounters()
    {
        _counters[0] = _counters[1] = 0;
        _counterConsul = 0;
    }

    private void GivenTheServicesAreRegisteredWithConsul(params ServiceEntry[] serviceEntries) => _consulServices.AddRange(serviceEntries);
    private void GivenTheServiceNodesAreRegisteredWithConsul(params Node[] nodes) => _consulNodes.AddRange(nodes);

#if NET7_0_OR_GREATER
    [GeneratedRegex("/v1/health/service/(?<serviceName>[^/]+)")]
    private static partial Regex ServiceNameRegex();
#else
    private static readonly Regex ServiceNameRegexVar = new("/v1/health/service/(?<serviceName>[^/]+)");
    private static Regex ServiceNameRegex() => ServiceNameRegexVar;
#endif
    private void GivenThereIsAFakeConsulServiceDiscoveryProvider(string url)
    {
        _consulHandler.GivenThereIsAServiceRunningOn(url, async context =>
        {
            if (context.Request.Headers.TryGetValue("X-Consul-Token", out var values))
            {
                _receivedToken = values.First();
            }

            // Parse the request path to get the service name
            var pathMatch = ServiceNameRegex().Match(context.Request.Path.Value);
            if (pathMatch.Success)
            {
                //string json;
                //lock (ConsulCounterLocker)
                //{
                //_counterConsul++;
                int count = Interlocked.Increment(ref _counterConsul);

                // Use the parsed service name to filter the registered Consul services
                var serviceName = pathMatch.Groups["serviceName"].Value;
                var services = _consulServices.Where(x => x.Service.Service == serviceName).ToList();
                var json = JsonConvert.SerializeObject(services);

                //}
                context.Response.Headers.Append("Content-Type", "application/json");
                await context.Response.WriteAsync(json);
                return;
            }

            if (context.Request.Path.Value == "/v1/catalog/nodes")
            {
                //_counterNodes++;
                int count = Interlocked.Increment(ref _counterNodes);
                var json = JsonConvert.SerializeObject(_consulNodes);
                context.Response.Headers.Append("Content-Type", "application/json");
                await context.Response.WriteAsync(json);
            }
        });
    }

    private void ThenConsulShouldHaveBeenCalledTimes(int expected) => _counterConsul.ShouldBe(expected);
    private void ThenConsulNodesShouldHaveBeenCalledTimes(int expected) => _counterNodes.ShouldBe(expected);
}
