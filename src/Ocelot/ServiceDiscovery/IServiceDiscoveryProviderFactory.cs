namespace Ocelot.ServiceDiscovery
{
    using Ocelot.Configuration;
    using Responses;
    using Providers;

    public interface IServiceDiscoveryProviderFactory
    {
        Response<IServiceDiscoveryProvider> Get(ServiceProviderConfiguration serviceConfig, DownstreamRoute route);
    }
}
