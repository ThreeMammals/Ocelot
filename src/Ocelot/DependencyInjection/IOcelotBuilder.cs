using System;
using System.Net.Http;
using Ocelot.Middleware.Multiplexer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Ocelot.DependencyInjection
{
    public interface IOcelotBuilder
    {
        IServiceCollection Services { get; }

        IConfiguration Configuration { get; }

        IOcelotBuilder AddDelegatingHandler<T>(bool global = false)
            where T : DelegatingHandler;

        IOcelotBuilder AddSingletonDefinedAggregator<T>() 
            where T : class, IDefinedAggregator;

        IOcelotBuilder AddTransientDefinedAggregator<T>() 
            where T : class, IDefinedAggregator;
    }
}
