using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq;

namespace Ocelot.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IOcelotBuilder AddOcelot(this IServiceCollection services,
            IConfigurationRoot configurationRoot)
        {
            return new OcelotBuilder(services, configurationRoot);
        }
    }
}
