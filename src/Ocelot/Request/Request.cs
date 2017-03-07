using System.Net.Http;
using Ocelot.Requester.QoS;

namespace Ocelot.Request
{
    public class Request
    {
        public Request(
            HttpRequestMessage httpRequestMessage, 
            bool isQos,
            IQoSProvider qosProvider)
        {
            HttpRequestMessage = httpRequestMessage;
            IsQos = isQos;
            QosProvider = qosProvider;
        }

        public HttpRequestMessage HttpRequestMessage { get; private set; }
        public bool IsQos { get; private set; }
        public IQoSProvider QosProvider { get; private set; }
    }
}
