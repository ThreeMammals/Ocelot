using Ocelot.Responses;
using Ocelot.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ocelot.UnitTests
{
    public class RegistryClientTest
    {
        private IServiceRegistry _serviceRegistry;
        private ILoadBalancer _roundRobin;
        private RegistryClient _client;

        public RegistryClientTest()
        {
            _roundRobin = new RoundRobin();
            var configuration = new ConsulRegistryConfiguration();
            _serviceRegistry = new ConsulServiceRegistry(configuration);
            _client = new RegistryClient(_roundRobin, _serviceRegistry);
        }

        [Fact]
        public void FindServicesAsync()
        {
            var services = _client.FindServiceInstances("consul");
            Assert.NotNull(services);
            Assert.True(services.Any());
            var instance = _client.Lease(services);
            Assert.Equal("8300", instance.Data.DownstreamPort.ToString());

        }
    }

    public class RegistryClient
    {
        private readonly ILoadBalancer _loadBlancer;
        private readonly IServiceRegistry _registryHost;

        public RegistryClient(ILoadBalancer loadBalancer, IServiceRegistry registryHost)
        {
            _loadBlancer = loadBalancer;
            _registryHost = registryHost;
        }

        public Response<HostAndPort> Lease(IList<Service> instances)
        {
            return _loadBlancer.Lease(instances);
        }

        public List<Service> FindServiceInstances(string name)
        {
            return _registryHost.Lookup(name);
        }
    }
}
