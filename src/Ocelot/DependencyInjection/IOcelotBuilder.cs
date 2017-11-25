using CacheManager.Core;
using System;

namespace Ocelot.DependencyInjection
{
    public interface IOcelotBuilder
    {
        IOcelotBuilder AddStoreOcelotConfigurationInConsul();
        IOcelotBuilder AddCacheManager(Action<ConfigurationBuilderCachePart> settings);
    }
}
