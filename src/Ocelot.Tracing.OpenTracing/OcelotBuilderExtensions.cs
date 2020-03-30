using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using System;

namespace Ocelot.Tracing.OpenTracing
{
    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddOpenTracing(this IOcelotBuilder builder)
        {
            builder.Services.AddSingleton<ITracer, OpenTracingTracer>();
            
            return builder;
        }
    }
}
