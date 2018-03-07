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

        [Obsolete("Please use IOcelotBuilder AddDelegatingHandler<T>() where T : DelegatingHandler, this will be removed anytime after 2018-03-06.")]
        IOcelotBuilder AddDelegatingHandler(Func<DelegatingHandler> delegatingHandler);

        IOcelotBuilder AddDelegatingHandler<T>() where T : DelegatingHandler;
    }
}
