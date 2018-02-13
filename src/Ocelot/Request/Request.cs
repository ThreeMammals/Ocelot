using System.Net.Http;
using Ocelot.Requester.QoS;

namespace Ocelot.Request
{
    public class Request
    {
        public Request(
            HttpRequestMessage httpRequestMessage, 
            bool isQos,
            IQoSProvider qosProvider, 
            bool allowAutoRedirect,
            bool useCookieContainer,
            string reRouteKey,
            bool isTracing
            )
        {
            HttpRequestMessage = httpRequestMessage;
            IsQos = isQos;
            QosProvider = qosProvider;
            AllowAutoRedirect = allowAutoRedirect;
            UseCookieContainer = useCookieContainer;
            ReRouteKey = reRouteKey;
            IsTracing = isTracing;
        }

        public HttpRequestMessage HttpRequestMessage { get; private set; }
        public bool IsQos { get; private set; }
        public bool IsTracing { get; private set; }
        public IQoSProvider QosProvider { get; private set; }
        public bool AllowAutoRedirect { get; private set; }
        public bool UseCookieContainer { get; private set; }
        public string ReRouteKey { get; private set; }
    }
}