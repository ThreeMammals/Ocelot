using Ocelot.Requester.QoS;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Ocelot.Request
{
    public class HttpRequest : Request
    {
        public HttpRequest(
           HttpRequestMessage httpRequestMessage,
           bool isQos,
           IQoSProvider qosProvider) : base(isQos, qosProvider)
        {
            HttpRequestMessage = httpRequestMessage;
        }

        public HttpRequestMessage HttpRequestMessage { get; private set; }
    }
}
