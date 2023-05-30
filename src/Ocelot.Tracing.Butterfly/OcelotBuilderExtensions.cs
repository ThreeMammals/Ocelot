﻿using System;

using Ocelot.DependencyInjection;

using global::Butterfly.Client.AspNetCore;

using Ocelot.Logging;

using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Tracing.Butterfly
{
    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddButterfly(this IOcelotBuilder builder, Action<ButterflyOptions> settings)
        {
            builder.Services.AddSingleton<ITracer, ButterflyTracer>();
            builder.Services.AddButterfly(settings);
            return builder;
        }
    }
}
