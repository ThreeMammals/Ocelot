using Ocelot.Configuration;
using Ocelot.Logging;
using System.Net.Http;

namespace Ocelot.Requester
{
    public delegate DelegatingHandler QosDelegatingHandlerDelegate(DownstreamRoute route, IOcelotLoggerFactory logger);
}
