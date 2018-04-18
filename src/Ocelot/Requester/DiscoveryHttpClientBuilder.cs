using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Ocelot.Configuration;
using Pivotal.Discovery.Client;

namespace Ocelot.Requester
{
    public class DiscoveryHttpClientBuilder : IDiscoveryHttpClientBuilder
    {
        public IHttpClient Create(DiscoveryHttpClientHandler handler, DownstreamReRoute request)
        {
            var client = new HttpClient(handler);
            return new HttpClientWrapper(client);
        }
    }
}
