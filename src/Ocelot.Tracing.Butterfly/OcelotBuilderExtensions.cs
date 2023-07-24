using Butterfly.Client.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Logging;

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
