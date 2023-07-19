using Ocelot.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

using Steeltoe.Discovery.Client;

namespace Ocelot.Provider.Eureka
{
    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddEureka(this IOcelotBuilder builder)
        {
            builder.Services.AddDiscoveryClient(builder.Configuration);
            builder.Services.AddSingleton(EurekaProviderFactory.Get);
            builder.Services.AddSingleton(EurekaMiddlewareConfigurationProvider.Get);
            return builder;
        }
    }
}
