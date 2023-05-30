using Ocelot.DependencyInjection;

using Ocelot.Logging;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ocelot.Tracing.OpenTracing
{
    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddOpenTracing(this IOcelotBuilder builder)
        {
            builder.Services.TryAddSingleton<ITracer, OpenTracingTracer>();
            return builder;
        }
    }
}
