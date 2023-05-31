// <copyright file="OcelotBuilderExtensions.cs" company="ThreeMammals">
// Copyright (c) ThreeMammals. All rights reserved.
// </copyright>

namespace Ocelot.Tracing.OpenTracing;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Logging;

public static class OcelotBuilderExtensions
{
    public static IOcelotBuilder AddOpenTracing(this IOcelotBuilder builder)
    {
        builder.Services.TryAddSingleton<ITracer, OpenTracingTracer>();
        return builder;
    }
}
