using CacheManager.Core;
using System;
using System.Net.Http;
using IdentityServer4.AccessTokenValidation;
using Ocelot.Middleware.Multiplexer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Ocelot.DependencyInjection
{
    public interface IOcelotBuilder
    {
        IServiceCollection Services { get; }
        IConfiguration Configuration { get; }
        IOcelotBuilder AddStoreOcelotConfigurationInConsul();

        IOcelotBuilder AddCacheManager(Action<ConfigurationBuilderCachePart> settings);

        IOcelotAdministrationBuilder AddAdministration(string path, string secret);

        IOcelotAdministrationBuilder AddAdministration(string path, Action<IdentityServerAuthenticationOptions> configOptions);

        IOcelotBuilder AddDelegatingHandler<T>(bool global = false)
            where T : DelegatingHandler;

        IOcelotBuilder AddSingletonDefinedAggregator<T>() 
            where T : class, IDefinedAggregator;
        IOcelotBuilder AddTransientDefinedAggregator<T>() 
            where T : class, IDefinedAggregator;
    }
}
