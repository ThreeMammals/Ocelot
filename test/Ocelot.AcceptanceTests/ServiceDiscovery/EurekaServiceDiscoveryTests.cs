using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure;
using Ocelot.LoadBalancer.LoadBalancers;
using Steeltoe.Common.Discovery;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ocelot.AcceptanceTests.ServiceDiscovery
{
    public class EurekaServiceDiscoveryTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly List<IServiceInstance> _eurekaInstances;
        private readonly ServiceHandler _serviceHandler;

        public EurekaServiceDiscoveryTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
            _eurekaInstances = new List<IServiceInstance>();
        }

        [Theory]
        [Trait("Feat", "262")]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_use_eureka_service_discovery_and_make_request(bool dotnetRunningInContainer)
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", dotnetRunningInContainer.ToString());
            var eurekaPort = 8761;
            var serviceName = "product";
            var downstreamServicePort = PortFinder.GetRandomPort();
            var downstreamServiceOneUrl = $"http://localhost:{downstreamServicePort}";
            var fakeEurekaServiceDiscoveryUrl = $"http://localhost:{eurekaPort}";

            var instanceOne = new FakeEurekaService(serviceName, "localhost", downstreamServicePort, false,
                new Uri($"http://localhost:{downstreamServicePort}"), new Dictionary<string, string>());

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
                        LoadBalancerOptions = new FileLoadBalancerOptions { Type = nameof(LeastConnection) },
                    },
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Type = "Eureka",
                    },
                },
            };

            this.Given(x => x.GivenEurekaProductServiceOneIsRunning(downstreamServiceOneUrl))
                .And(x => x.GivenThereIsAFakeEurekaServiceDiscoveryProvider(fakeEurekaServiceDiscoveryUrl, serviceName))
                .And(x => x.GivenTheServicesAreRegisteredWithEureka(instanceOne))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithEureka())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(_ => _steps.ThenTheResponseBodyShouldBe(nameof(EurekaServiceDiscoveryTests)))
                .BDDfy();
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
            _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
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
                                new()
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
                                        serviceUpTimestamp = 1457714988223,
                                    },
                                    metadata = new()
                                    {
                                        value = "java.util.Collections$EmptyMap",
                                    },
                                    homePageUrl = $"{serviceInstance.Host}:{serviceInstance.Port}",
                                    statusPageUrl = $"{serviceInstance.Host}:{serviceInstance.Port}",
                                    healthCheckUrl = $"{serviceInstance.Host}:{serviceInstance.Port}",
                                    vipAddress = serviceName,
                                    isCoordinatingDiscoveryServer = "false",
                                    lastUpdatedTimestamp = "1457714988223",
                                    lastDirtyTimestamp = "1457714988172",
                                    actionType = "ADDED",
                                },
                            },
                        };

                        apps.Add(a);
                    }

                    var applications = new EurekaApplications
                    {
                        applications = new Applications
                        {
                            application = apps,
                            apps__hashcode = "UP_1_",
                            versions__delta = "1",
                        },
                    };

                    var json = JsonSerializer.Serialize(applications, JsonSerializerOptionsFactory.Web);
                    context.Response.Headers.Append("Content-Type", "application/json");
                    await context.Response.WriteAsync(json);
                }
            });
        }

        private void GivenEurekaProductServiceOneIsRunning(string url)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
            {
                try
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync(nameof(EurekaServiceDiscoveryTests));
                }
                catch (Exception exception)
                {
                    await context.Response.WriteAsync(exception.StackTrace);
                }
            });
        }

        public void Dispose()
        {
            _serviceHandler?.Dispose();
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
        [JsonPropertyName("$")]
        public int value { get; set; }

        [JsonPropertyName("@enabled")]
        public string enabled { get; set; }
    }

    public class SecurePort
    {
        [JsonPropertyName("$")]
        public int value { get; set; }

        [JsonPropertyName("@enabled")]
        public string enabled { get; set; }
    }

    public class DataCenterInfo
    {
        [JsonPropertyName("@class")]
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

    public class ValueMetadata
    {
        [JsonPropertyName("@class")]
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
        public ValueMetadata metadata { get; set; }
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
