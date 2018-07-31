namespace Ocelot.Butterfly
{
    using System;
    using DependencyInjection;
    using global::Butterfly.Client.AspNetCore;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

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
