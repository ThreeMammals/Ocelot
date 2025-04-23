using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration;
using Ocelot.Configuration.Repository;
using Ocelot.Middleware;

namespace Ocelot.Provider.Nacos;

public class NacosMiddlewareConfigurationProvider
{
    public static OcelotMiddlewareConfigurationDelegate Get { get; } = builder =>
    {
        var internalConfigRepo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();
        var log =builder.ApplicationServices.GetService<ILogger<NacosMiddlewareConfigurationProvider>>();
        var config = internalConfigRepo.Get();

        if (UsingNacosServiceDiscoveryProvider(config.Data))
        {
            log.LogInformation("Using Nacos service discovery provider.");
        }

        return Task.CompletedTask;
    };

    private static bool UsingNacosServiceDiscoveryProvider(IInternalConfiguration configuration)
    {
        return configuration?.ServiceProviderConfiguration != null 
               && configuration.ServiceProviderConfiguration.Type?.ToLower() == "nacos";
    }
}
