using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Configuration
{
    /// <summary>
    /// Describes options for retrieving reroute configuration 
    /// when using service discovery for dynamic routing
    /// </summary>
    public class DynamicReRouteConfiguration
    {
        public DynamicReRouteConfiguration(string store, string host, string port)
        {
            Store = store;
            Host = host;
            Port = port;
        }

        /// <summary>
        /// This property defines the provider to use to get the dynamic configuration.
        /// This, in turn, means the store (persistance) to use.
        /// </summary>
        public string Store { get; }

        /// <summary>
        /// The host of the store.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// The port on <see cref="Host"/> where the store is.
        /// </summary>
        public string Port { get; }
    }
}
