using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.DependencyInjection
{
    /// <summary>
    /// Ocelot helper class for DI configuration
    /// </summary>
    public class OcelotBuilder : IOcelotBuilder
    {
        private IServiceCollection _services;
        private IConfigurationRoot _configurationRoot;
        
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>
        /// The services.
        /// </value>
        public IServiceCollection Services { get; }

        public IConfigurationRoot Configuration { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OcelotBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        public OcelotBuilder(IServiceCollection services, IConfigurationRoot configuration)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
    }
}
