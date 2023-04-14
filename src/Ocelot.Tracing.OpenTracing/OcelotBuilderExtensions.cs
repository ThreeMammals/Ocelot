namespace Ocelot.Tracing.OpenTracing
{
    using Microsoft.Extensions.DependencyInjection.Extensions;

    using DependencyInjection;
    using Logging;

    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddOpenTracing(this IOcelotBuilder builder)
        {
            builder.Services.TryAddSingleton<ITracer, OpenTracingTracer>();
            return builder;
        }
    }
}
