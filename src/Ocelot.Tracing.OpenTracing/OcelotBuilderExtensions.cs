using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using OpenTracing.Util;

namespace Ocelot.Tracing.OpenTracing;

/// <summary>
/// Extension methods for the <see cref="IOcelotBuilder"/> interface.
/// </summary>
public static class OcelotBuilderExtensions
{
    /// <summary>
    /// Adds OpenTracing services using builder.
    /// </summary>
    /// <param name="builder">The Ocelot builder with services.</param>
    /// <returns>An <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddOpenTracing(this IOcelotBuilder builder)
    {
        builder.Services
            .AddSingleton<IOcelotTracer, OpenTracingTracer>()
            .AddSingleton<ITracer>(GlobalTracer.Instance);
        return builder;
    }
}
