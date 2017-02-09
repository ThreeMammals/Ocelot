using Ocelot.Configuration;
using Ocelot.Values;
using System.Net;
using System.Net.Http;

namespace Ocelot.Request
{
    public class Request
    {
        public Request(HttpRequestMessage httpRequestMessage, CookieContainer cookieContainer,bool isQos, QoSOptions qos)
        {
            HttpRequestMessage = httpRequestMessage;
            CookieContainer = cookieContainer;
            IsQos = isQos;
            Qos = qos;
        }

        public HttpRequestMessage HttpRequestMessage { get; private set; }
        public CookieContainer CookieContainer { get; private set; }
        public bool IsQos { get; private set; }
        public QoSOptions Qos { get; private set; }
    }
}
