using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Logging;
using Ocelot.Requester.QoS;
using System.Net;

namespace Ocelot.Requester
{
    public interface IHttpClientBuilder
    {

        /// <summary>
        /// Sets a PollyCircuitBreakingDelegatingHandler .
        /// </summary>
        IHttpClientBuilder WithQos(IQoSProvider qosProvider, IOcelotLogger logger);            

        /// <summary>
        /// Creates the <see cref="HttpClient"/>
        /// </summary>
        IHttpClient Create();
    }
}
