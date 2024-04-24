using KubeClient;
using KubeClient.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.UnitTests.Kubernetes
{
    public class KubeTests : IDisposable
    {
        private IWebHost _fakeKubeBuilder;
        private readonly Kube _provider;
        private EndpointsV1 _endpointEntries;
        private readonly string _serviceName;
        private readonly string _namespaces;
        private readonly int _port;
        private readonly string _kubeHost;
        private readonly string _fakekubeServiceDiscoveryUrl;
        private List<Service> _services;
        private string _receivedToken;
        private readonly Mock<IOcelotLoggerFactory> _factory;
        private readonly Mock<IOcelotLogger> _logger;
        private readonly IKubeApiClient _clientFactory;
        private readonly Mock<IKubeServiceBuilder> _serviceBuilder;

        public KubeTests()
        {
            _serviceName = "test";
            _namespaces = "dev";
            _port = 5567;
            _kubeHost = "localhost";
            _fakekubeServiceDiscoveryUrl = $"{Uri.UriSchemeHttp}://{_kubeHost}:{_port}";
            _endpointEntries = new();
            _factory = new();

            var option = new KubeClientOptions
            {
                ApiEndPoint = new Uri(_fakekubeServiceDiscoveryUrl),
                AccessToken = "txpc696iUhbVoudg164r93CxDTrKRVWG",
                AuthStrategy = KubeAuthStrategy.BearerToken,
                AllowInsecure = true,
            };

            _clientFactory = KubeApiClient.Create(option);
            _logger = new();
            _factory.Setup(x => x.CreateLogger<Kube>()).Returns(_logger.Object);
            var config = new KubeRegistryConfiguration
            {
                KeyOfServiceInK8s = _serviceName,
                KubeNamespace = _namespaces,
            };
            _serviceBuilder = new();
            _provider = new Kube(config, _factory.Object, _clientFactory, _serviceBuilder.Object);
        }

        [Fact]
        public void Should_return_service_from_k8s()
        {
            // Arrange
            var token = "Bearer txpc696iUhbVoudg164r93CxDTrKRVWG";
            var endPointEntryOne = new EndpointsV1
            {
                Kind = "endpoint",
                ApiVersion = "1.0",
                Metadata = new ObjectMetaV1
                {
                    Name = nameof(Should_return_service_from_k8s),
                    Namespace = "dev",
                },
            };
            var endpointSubsetV1 = new EndpointSubsetV1();
            endpointSubsetV1.Addresses.Add(new EndpointAddressV1
            {
                Ip = "127.0.0.1",
                Hostname = "localhost",
            });
            endpointSubsetV1.Ports.Add(new EndpointPortV1
            {
                Port = 80,
            });
            endPointEntryOne.Subsets.Add(endpointSubsetV1);
            _serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
                .Returns(new Service[] { new(nameof(Should_return_service_from_k8s), new("localhost", 80), string.Empty, string.Empty, new string[0]) });
            GivenThereIsAFakeKubeServiceDiscoveryProvider(_fakekubeServiceDiscoveryUrl, _serviceName, _namespaces);
            GivenTheServicesAreRegisteredWithKube(endPointEntryOne);

            // Act
            WhenIGetTheServices();

            // Assert
            ThenTheCountIs(1);
            ThenTheTokenIs(token);
        }

        private void ThenTheTokenIs(string token)
        {
            _receivedToken.ShouldBe(token);
        }

        private void ThenTheCountIs(int count)
        {
            _services.Count.ShouldBe(count);
        }

        private void WhenIGetTheServices()
        {
            _services = _provider.GetAsync().GetAwaiter().GetResult();
        }

        private void GivenTheServicesAreRegisteredWithKube(EndpointsV1 endpointEntries)
        {
            _endpointEntries = endpointEntries;
        }

        private void GivenThereIsAFakeKubeServiceDiscoveryProvider(string url, string serviceName, string namespaces)
        {
            _fakeKubeBuilder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        if (context.Request.Path.Value == $"/api/v1/namespaces/{namespaces}/endpoints/{serviceName}")
                        {
                            if (context.Request.Headers.TryGetValue("Authorization", out var values))
                            {
                                _receivedToken = values.First();
                            }

                            var json = JsonConvert.SerializeObject(_endpointEntries);
                            context.Response.Headers.Append("Content-Type", "application/json");
                            await context.Response.WriteAsync(json);
                        }
                    });
                })
                .Build();

            _fakeKubeBuilder.Start();
        }

        public void Dispose()
        {
            _fakeKubeBuilder?.Dispose();
        }
    }
}
