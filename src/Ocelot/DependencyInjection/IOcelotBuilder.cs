using Butterfly.Client.AspNetCore;
using CacheManager.Core;
using System;
using System.Net.Http;
using IdentityServer4.AccessTokenValidation;
using Ocelot.Middleware.Multiplexer;

namespace Ocelot.DependencyInjection
{
    public interface IOcelotBuilder
    {
        IOcelotBuilder AddStoreOcelotConfigurationInConsul();

        IOcelotBuilder AddCacheManager(Action<ConfigurationBuilderCachePart> settings);

        IOcelotBuilder AddOpenTracing(Action<ButterflyOptions> settings);

        IOcelotAdministrationBuilder AddAdministration(string path, string secret);

        IOcelotAdministrationBuilder AddAdministration(string path, Action<IdentityServerAuthenticationOptions> configOptions);

        IOcelotBuilder AddSingletonDelegatingHandler<T>(bool global = false)
            where T : DelegatingHandler;

        IOcelotBuilder AddTransientDelegatingHandler<T>(bool global = false)
            where T : DelegatingHandler;

        IOcelotBuilder AddSingletonDefinedAggregator<T>() 
            where T : class, IDefinedAggregator;
        IOcelotBuilder AddTransientDefinedAggregator<T>() 
            where T : class, IDefinedAggregator;
    }
}
