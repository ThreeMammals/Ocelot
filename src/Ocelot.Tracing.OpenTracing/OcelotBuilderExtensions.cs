// <copyright file="OcelotBuilderExtensions.cs" company="ThreeMammals">
// Copyright (c) ThreeMammals. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.DependencyInjection;

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
        builder.Services.TryAddSingleton<Logging.ITracer, OpenTracingTracer>();
        return builder;
    }
}
