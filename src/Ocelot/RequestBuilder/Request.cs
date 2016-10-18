namespace Ocelot.Library.RequestBuilder
{
    using System.Net;
    using System.Net.Http;

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
