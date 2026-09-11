using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Logging;

namespace Ocelot.QualityOfService;

public class NoQosDelegatingHandler : DelegatingHandler
{ }

internal class QosDelegatingHandler : DelegatingHandler
{
    public static DelegatingHandler Create(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
        => new QosDelegatingHandler();
}
