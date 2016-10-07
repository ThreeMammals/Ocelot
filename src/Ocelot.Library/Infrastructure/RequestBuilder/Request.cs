using System.Net;
using System.Net.Http;

namespace Ocelot.Library.Infrastructure.RequestBuilder
{
    public class Request
    {
        public Request(HttpRequestMessage httpRequestMessage, CookieContainer cookieContainer)
        {
            HttpRequestMessage = httpRequestMessage;
            CookieContainer = cookieContainer;
        }

        public HttpRequestMessage HttpRequestMessage { get; private set; }
        public CookieContainer CookieContainer { get; private set; }
    }
}
