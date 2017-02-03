using Ocelot.Values;
using System.Net;
using System.Net.Http;

namespace Ocelot.Request
{
    public class Request
    {
        public Request(HttpRequestMessage httpRequestMessage, CookieContainer cookieContainer, QoS qos)
        {
            HttpRequestMessage = httpRequestMessage;
            CookieContainer = cookieContainer;
            Qos = qos;
        }

        public HttpRequestMessage HttpRequestMessage { get; private set; }
        public CookieContainer CookieContainer { get; private set; }
        public QoS Qos { get; private set; }
    }
}
