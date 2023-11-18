using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.Provider.Consul;

public static class ConsulMiddlewareConfigurationProvider
{
    public static OcelotMiddlewareConfigurationDelegate Get { get; } = GetAsync;

    private static async Task GetAsync(IApplicationBuilder builder)
    {
        var fileConfigRepo = builder.ApplicationServices.GetService<IFileConfigurationRepository>();
        var fileConfig = builder.ApplicationServices.GetService<IOptionsMonitor<FileConfiguration>>();
        var internalConfigCreator = builder.ApplicationServices.GetService<IInternalConfigurationCreator>();
        var internalConfigRepo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();

        if (UsingConsul(fileConfigRepo))
        {
            await SetFileConfigInConsul(builder, fileConfigRepo, fileConfig, internalConfigCreator, internalConfigRepo);
        }
    }

    private static bool UsingConsul(IFileConfigurationRepository fileConfigRepo)
        => fileConfigRepo.GetType() == typeof(ConsulFileConfigurationRepository);

    private static async Task SetFileConfigInConsul(IApplicationBuilder builder,
        IFileConfigurationRepository fileConfigRepo, IOptionsMonitor<FileConfiguration> fileConfig,
        IInternalConfigurationCreator internalConfigCreator, IInternalConfigurationRepository internalConfigRepo)
    {
        // Get the config from Consul
        var fileConfigFromConsul = await fileConfigRepo.Get();
        if (IsError(fileConfigFromConsul))
        {
            ThrowToStopOcelotStarting(fileConfigFromConsul);
        }
        else if (ConfigNotStoredInConsul(fileConfigFromConsul))
        {
            // there was no config in Consul set the file in config in Consul
            await fileConfigRepo.Set(fileConfig.CurrentValue);
        }
        else
        {
            // Create the internal config from Consul data
            var internalConfig = await internalConfigCreator.Create(fileConfigFromConsul.Data);
            if (IsError(internalConfig))
            {
                ThrowToStopOcelotStarting(internalConfig);
            }
            else
            {
                // add the internal config to the internal repo
                var response = internalConfigRepo.AddOrReplace(internalConfig.Data);
                if (IsError(response))
                {
                    ThrowToStopOcelotStarting(response);
                }
            }

            if (IsError(internalConfig))
            {
                ThrowToStopOcelotStarting(internalConfig);
            }
        }
    }

    private static void ThrowToStopOcelotStarting(Response config)
        => throw new Exception($"Unable to start Ocelot, errors are: {string.Join(',', config.Errors.Select(x => x.ToString()))}");

    private static bool IsError(Response response)
        => response == null || response.IsError;

    private static bool ConfigNotStoredInConsul(Response<FileConfiguration> fileConfigFromConsul)
        => fileConfigFromConsul.Data == null;
}
