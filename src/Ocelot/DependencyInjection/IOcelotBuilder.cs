using Butterfly.Client.AspNetCore;
using CacheManager.Core;
using System;
using System.Net.Http;
using IdentityServer4.AccessTokenValidation;
using Ocelot.Requester;

namespace Ocelot.DependencyInjection
{
    public interface IOcelotBuilder
    {
        IOcelotBuilder AddStoreOcelotConfigurationInConsul();

        IOcelotBuilder AddCacheManager(Action<ConfigurationBuilderCachePart> settings);

        IOcelotBuilder AddOpenTracing(Action<ButterflyOptions> settings);

        IOcelotAdministrationBuilder AddAdministration(string path, string secret);

        IOcelotAdministrationBuilder AddAdministration(string path, Action<IdentityServerAuthenticationOptions> configOptions);

        IOcelotBuilder AddDelegatingHandler<T>(bool global = true) where T : DelegatingHandler;
        
        IOcelotBuilder AddTransientDelegatingHandler<T>(bool global = true) where T : DelegatingHandler;
    }
}
