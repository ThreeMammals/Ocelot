﻿namespace Ocelot.Provider.Eureka
{
    using System.Threading.Tasks;
    using Configuration;
    using Configuration.Repository;
    using Microsoft.Extensions.DependencyInjection;
    using Middleware;
    using Pivotal.Discovery.Client;

    public class EurekaMiddlewareConfigurationProvider
    {
        public static OcelotMiddlewareConfigurationDelegate Get = builder =>
        {
            var internalConfigRepo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();

            var config = internalConfigRepo.Get();

            if (UsingEurekaServiceDiscoveryProvider(config.Data))
            {
                builder.UseDiscoveryClient();
            }

            return Task.CompletedTask;
        };

        private static bool UsingEurekaServiceDiscoveryProvider(IInternalConfiguration configuration)
        {
            return configuration?.ServiceProviderConfiguration != null && configuration.ServiceProviderConfiguration.Type?.ToLower() == "eureka";
        }
    }
}
