using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Ocelot.DependencyInjection
{
    public interface IOcelotBuilder
    {
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>
        /// The services.
        /// </value>
        IServiceCollection Services { get; }
        IConfigurationRoot Configuration { get; }
    }
}
