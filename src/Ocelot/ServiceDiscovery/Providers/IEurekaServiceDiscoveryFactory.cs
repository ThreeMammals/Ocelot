namespace Ocelot.ServiceDiscovery.Providers
{
    using Pivotal.Discovery.Client;

    public interface IEurekaServiceDiscoveryFactory
    {
        IDiscoveryClient Get();
    }
}
