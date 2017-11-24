using System;
using CacheManager.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IOcelotBuilder AddOcelot(this IServiceCollection services,
            IConfigurationRoot configurationRoot)
        {
            var builder = new OcelotBuilder(services, configurationRoot);

            //add default cache settings...
            Action<ConfigurationBuilderCachePart> defaultCachingSettings = x =>
            {
                x.WithDictionaryHandle();
            };

            builder.AddCacheManager(defaultCachingSettings);

            //add ocelot services...
            builder
                .AddRequiredBaseServices()
                .AddQos()
                .AddServiceDiscovery()
                .AddLoadBalancer()
                .AddLogging()
                .AddRateLimitCounter();

            //add identity server for admin area
            builder.AddIdentityServer();

            //add asp.net services..
            builder.AddAspNetCoreServices();

            return builder;
        }
    }
}
