namespace Ocelot.ServiceDiscovery.Providers
{
    using Steeltoe.Common.Discovery;

    public interface IEurekaServiceDiscoveryFactory
    {
        IDiscoveryClient Get();
    }
}
