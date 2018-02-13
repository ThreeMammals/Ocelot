using System.Net.Http;
using Ocelot.Configuration;

namespace Ocelot.Requester
{
    public interface IHttpClientBuilder
    {
        /// <summary>
        /// Creates the <see cref="HttpClient"/>
        /// </summary>
        /// <param name="request"></param>
        IHttpClient Create(Request.Request request);
    }
}
