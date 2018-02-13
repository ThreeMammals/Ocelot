using Butterfly.Client.AspNetCore;
using CacheManager.Core;
using System;
using System.Net.Http;

namespace Ocelot.DependencyInjection
{
    public interface IOcelotBuilder
    {
        IOcelotBuilder AddStoreOcelotConfigurationInConsul();
        IOcelotBuilder AddCacheManager(Action<ConfigurationBuilderCachePart> settings);
        IOcelotBuilder AddOpenTracing(Action<ButterflyOptions> settings);      
        IOcelotAdministrationBuilder AddAdministration(string path, string secret);
        IOcelotBuilder AddDelegatingHandler(Func<DelegatingHandler> delegatingHandler);
    }
}
