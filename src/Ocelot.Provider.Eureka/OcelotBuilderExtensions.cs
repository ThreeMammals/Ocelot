using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Steeltoe.Discovery.Client;

namespace Ocelot.Provider.Eureka
{
    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddEureka(this IOcelotBuilder builder)
        {
            builder.Services
                .AddDiscoveryClient(builder.Configuration)
                .AddSingleton(EurekaProviderFactory.Get)
                .AddSingleton(EurekaMiddlewareConfigurationProvider.Get);
            return builder;
        }
    }
}
