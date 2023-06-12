using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Ocelot.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IOcelotBuilder AddOcelot(this IServiceCollection services)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        return new OcelotBuilder(services, configuration);
    }

    public static IOcelotBuilder AddOcelot(this IServiceCollection services, IConfiguration configuration)
    {
        return new OcelotBuilder(services, configuration);
    }

    public static IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        return new OcelotBuilder(services, configuration, customBuilder);
    }

    public static IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, IConfiguration configuration, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)
    {
        return new OcelotBuilder(services, configuration, customBuilder);
    }
}
