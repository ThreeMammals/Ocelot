using CacheManager.Core;
using System;
using System.Net.Http;

namespace Ocelot.DependencyInjection
{
    public interface IOcelotBuilder
    {
        IOcelotBuilder AddStoreOcelotConfigurationInConsul();
        IOcelotBuilder AddCacheManager(Action<ConfigurationBuilderCachePart> settings);
        IOcelotAdministrationBuilder AddAdministration(string path, string secret);
        IOcelotBuilder AddDelegatingHandler(DelegatingHandler delegatingHandler);
    }
}
