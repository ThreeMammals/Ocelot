namespace Ocelot.ServiceDiscovery
{
    using Ocelot.Configuration;

    using Providers;

    using Responses;

    public interface IServiceDiscoveryProviderFactory
    {
        Response<IServiceDiscoveryProvider> Get(ServiceProviderConfiguration serviceConfig, DownstreamRoute route);
    }
}
