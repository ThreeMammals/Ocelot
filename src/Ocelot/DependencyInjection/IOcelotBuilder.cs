using Butterfly.Client.AspNetCore;
using CacheManager.Core;
using System;

namespace Ocelot.DependencyInjection
{
    public interface IOcelotBuilder
    {
        IOcelotBuilder AddStoreOcelotConfigurationInConsul();
        IOcelotBuilder AddCacheManager(Action<ConfigurationBuilderCachePart> settings);
        IOcelotBuilder AddOpenTracing(Action<ButterflyOptions> settings);      
        IOcelotAdministrationBuilder AddAdministration(string path, string secret);
    }
}
