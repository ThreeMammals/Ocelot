using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Requester.QoS;

namespace Ocelot.Requester.QoS
{
    public delegate IQoSProvider QosProviderDelegate(DownstreamReRoute reRoute, IOcelotLoggerFactory factory);
}
