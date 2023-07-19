using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.DependencyInjection
{
    public class OcelotAdministrationBuilder : IOcelotAdministrationBuilder
    {
        public IServiceCollection Services { get; }
        public IConfiguration ConfigurationRoot { get; }

        public OcelotAdministrationBuilder(IServiceCollection services, IConfiguration configurationRoot)
        {
            ConfigurationRoot = configurationRoot;
            Services = services;
        }
    }
}
