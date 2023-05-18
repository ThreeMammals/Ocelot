namespace Ocelot.Requester
{
    using System.Net.Http;

    using Configuration;

    using Logging;

    public delegate DelegatingHandler QosDelegatingHandlerDelegate(DownstreamRoute route, IOcelotLoggerFactory logger);
}
