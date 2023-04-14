namespace Ocelot.Tracing.OpenTracing
{
    using DependencyInjection;

    using Logging;

    using Microsoft.Extensions.DependencyInjection.Extensions;

    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddOpenTracing(this IOcelotBuilder builder)
        {
            builder.Services.TryAddSingleton<ITracer, OpenTracingTracer>();
            return builder;
        }
    }
}
