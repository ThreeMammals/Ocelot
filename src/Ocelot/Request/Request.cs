using Ocelot.Configuration;
using Ocelot.Values;
using System.Net;
using System.Net.Http;
using Ocelot.Requester.QoS;

namespace Ocelot.Request
{
    public class Request
    {
        public Request(
            HttpRequestMessage httpRequestMessage, 
            CookieContainer cookieContainer,
            bool isQos,
            IQoSProvider qosProvider)
        {
            HttpRequestMessage = httpRequestMessage;
            CookieContainer = cookieContainer;
            IsQos = isQos;
            QosProvider = qosProvider;
        }

        public HttpRequestMessage HttpRequestMessage { get; private set; }
        public CookieContainer CookieContainer { get; private set; }
        public bool IsQos { get; private set; }
        public IQoSProvider QosProvider { get; private set; }
    }
}
