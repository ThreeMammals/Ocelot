namespace Ocelot.ServiceDiscovery
{
    public interface IServiceProviderFactory
    {
        IServiceProvider Get(ServiceConfiguraion serviceConfig);
    }
}