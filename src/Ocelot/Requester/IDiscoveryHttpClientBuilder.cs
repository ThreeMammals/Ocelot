using Ocelot.Configuration;
using Pivotal.Discovery.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Requester
{
    public interface IDiscoveryHttpClientBuilder
    {
        /// <summary>
        /// Creates the <see cref="HttpClient"/>
        /// </summary>
        /// <param name="handler">Discovery handler</param>
        /// <param name="request">Downstream Route request</param>
        /// <returns>IHttpClient</returns>
        IHttpClient Create(DiscoveryHttpClientHandler handler, DownstreamReRoute request);
    }
}
