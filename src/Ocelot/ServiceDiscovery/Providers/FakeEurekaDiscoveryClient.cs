namespace Ocelot.ServiceDiscovery.Providers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pivotal.Discovery.Client;

    public class FakeEurekaDiscoveryClient : IDiscoveryClient
    {
        public IServiceInstance GetLocalServiceInstance()
        {
            throw new System.NotImplementedException();
        }

        public IList<IServiceInstance> GetInstances(string serviceId)
        {
            throw new System.NotImplementedException();
        }

        public Task ShutdownAsync()
        {
            throw new System.NotImplementedException();
        }

        public string Description { get; }
        public IList<string> Services { get; }
    }
}
