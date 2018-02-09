using System.Net.Http;
using Ocelot.Configuration;

namespace Ocelot.Requester
{
    public interface IHttpClientBuilder
    {
        /// <summary>
        /// Creates the <see cref="HttpClient"/>
        /// </summary>
        /// <param name="useCookies">Defines if http client should use cookie container</param>
        /// <param name="allowAutoRedirect">Defines if http client should allow auto redirect</param>
        /// <param name="request"></param>
        /// <param name="reRoute"></param>
        IHttpClient Create(bool useCookies, bool allowAutoRedirect, Request.Request request);
    }
}
