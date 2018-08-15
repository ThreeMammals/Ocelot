using System.Net.Http;
using Ocelot.Logging;
using Ocelot.Requester.QoS;

namespace Ocelot.Requester
{
    public delegate DelegatingHandler LastDelegatingHandlerDelegate(IQoSProvider provider, IOcelotLoggerFactory factory);
}
