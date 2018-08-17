using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Requester.QoS;

namespace Ocelot.Requester
{
    public delegate DelegatingHandler LastDelegatingHandlerDelegate(QosProviderDelegate provider, DownstreamReRoute reRoute, IOcelotLoggerFactory factory);
}
