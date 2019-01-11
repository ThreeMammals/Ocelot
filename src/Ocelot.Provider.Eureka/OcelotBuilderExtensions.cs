namespace Ocelot.Provider.Eureka
{
    using System.Linq;
    using DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Middleware;
    using Pivotal.Discovery.Client;
    using ServiceDiscovery;

    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddEureka(this IOcelotBuilder builder)
        {
            var service = builder.Services.First(x => x.ServiceType == typeof(IConfiguration));
            var configuration = (IConfiguration)service.ImplementationInstance;
            builder.Services.AddDiscoveryClient(configuration);
            builder.Services.AddSingleton<ServiceDiscoveryFinderDelegate>(EurekaProviderFactory.Get);
            builder.Services.AddSingleton<OcelotMiddlewareConfigurationDelegate>(EurekaMiddlewareConfigurationProvider.Get);
            return builder;
        }
    }
}
