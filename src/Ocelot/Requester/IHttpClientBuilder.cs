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
        /// Sets the cookie container used to store server cookies by the handler
        /// </summary>
        /// <param name="cookieContainer"></param>
        /// <returns></returns>
        IHttpClientBuilder WithCookieContainer(CookieContainer cookieContainer);

        /// <summary>
        /// Sets the number of milliseconds to wait before the request times out.
        /// </summary>
        IHttpClientBuilder WithTimeout(TimeSpan timeout);

        /// <summary>
        /// Sets a DelegatingHandler.
        /// Can be reused to set several different Handlers in a pipeline.
        /// </summary>
        IHttpClientBuilder WithHandler(DelegatingHandler handler);     

        /// <summary>
        /// Sets Default HttpRequestHeaders
        /// </summary>
        IHttpClientBuilder WithDefaultRequestHeaders(Dictionary<string, string> headers);

        /// <summary>
        /// Creates the <see cref="HttpClient"/>
        /// </summary>
        IHttpClient Create();
    }
}
