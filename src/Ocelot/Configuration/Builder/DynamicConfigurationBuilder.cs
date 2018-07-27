using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Configuration.Builder
{
    public class DynamicConfigurationBuilder
    {
        private string _store;

        private string _host;

        private string _port;

        public DynamicConfigurationBuilder WithStore(string store)
        {
            _store = store;
            return this;
        }

        public DynamicConfigurationBuilder WithServer(string host, int port)
        {
            _host = host;
            _port = port.ToString();
            return this;
        }

        public DynamicReRouteConfiguration Build()
        {
            return new DynamicReRouteConfiguration(_store, _host, _port);
        }
    }
}
