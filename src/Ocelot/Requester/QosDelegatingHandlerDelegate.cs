namespace Ocelot.Requester
{
    using Configuration;
    using Logging;
    using System.Net.Http;

    public delegate DelegatingHandler QosDelegatingHandlerDelegate(DownstreamReRoute reRoute, IOcelotLoggerFactory logger);
}
