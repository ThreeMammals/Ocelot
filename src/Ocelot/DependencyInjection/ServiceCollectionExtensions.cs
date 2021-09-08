using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Ocelot.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {

        public static IOcelotBuilder AddOcelot(this IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider()
                .GetRequiredService<IConfiguration>();
            return new OcelotBuilder(services, configuration);
        }

        public static IOcelotBuilder AddOcelot(this IServiceCollection services, IConfiguration configuration)
        {
            return new OcelotBuilder(services, configuration);
        }

        public static IOcelotBuilder AddOcelotWithCustomMvcCoreBuilder(this IServiceCollection services, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customMvcCoreBuilder)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            return new OcelotBuilder(services, configuration, customMvcCoreBuilder);
        }

        public static IOcelotBuilder AddOcelotWithCustomMvcCoreBuilder(this IServiceCollection services, IConfiguration configuration, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customMvcCoreBuilder)
        {
            return new OcelotBuilder(services, configuration, customMvcCoreBuilder);
        }
    }
}
