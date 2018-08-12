using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.DependencyInjection
{
    public class OcelotAdministrationBuilder : IOcelotAdministrationBuilder
    {
        private IServiceCollection Services { get; }
        private IConfiguration ConfigurationRoot { get; }

        public OcelotAdministrationBuilder(IServiceCollection services, IConfiguration configurationRoot)
        {
            ConfigurationRoot = configurationRoot;
            Services = services;    
        }
    }
}
