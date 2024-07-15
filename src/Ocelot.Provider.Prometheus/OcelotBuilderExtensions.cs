using Ocelot.DependencyInjection;
using Prometheus;

namespace Ocelot.Provider.Prometheus;

public static class OcelotBuilderExtensions
{
    public static IOcelotBuilder AddPrometheus(this IOcelotBuilder builder)
    {
        builder.Services.UseHttpClientMetrics();
        return builder;
    }

}
